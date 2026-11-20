using System.Text.Json;
using System.Threading.Channels;
using AdPulse.Api.Data;
using AdPulse.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdPulse.Api.Services;

/// <summary>进程内 AI 任务队列（计划 8.2）：发布接口只投递任务，绝不阻塞等待 AI。</summary>
public class AiTaskQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();
    public void Enqueue(int newsId) => _channel.Writer.TryWrite(newsId);
    public ChannelReader<int> Reader => _channel.Reader;
}

/// <summary>
/// 后台生成服务：单读者顺序消费，避免 SQLite 写竞争。
/// 无密钥 → 规则摘要(Fallback)；有密钥 → AI(Done)，失败重试 1 次后 Failed + 规则兜底 + Serilog 警告。
/// AI 标签自动合并进 Tags / NewsTags（幂等）。
/// </summary>
public class AiGenerationService(
    AiTaskQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<AiGenerationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var newsId in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var ai = scope.ServiceProvider.GetRequiredService<IAiSummaryService>();
                var n = await db.News.Include(x => x.NewsTags)
                    .FirstOrDefaultAsync(x => x.Id == newsId, stoppingToken);
                if (n is null || n.Status == NewsStatus.Recycled) continue; // 草稿也可生成，回收站跳过

                AiResult result;
                string status;
                if (!ai.Enabled)
                {
                    result = RuleEngine.Generate(n.Title, n.Content);
                    status = AiStatus.Fallback;
                }
                else
                {
                    try
                    {
                        result = await GenerateWithRetryAsync(ai, n, stoppingToken);
                        status = AiStatus.Done;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogWarning(ex, "AI 摘要生成失败（新闻#{NewsId}），已降级为规则摘要，主流程不受影响", newsId);
                        result = n.AiStatus == AiStatus.Done && !string.IsNullOrWhiteSpace(n.AiSummary)
                            ? new AiResult(n.AiSummary!, PortalService.ParseAiTags(n.AiTagsJson))
                            : RuleEngine.Generate(n.Title, n.Content);
                        status = AiStatus.Failed;
                    }
                }

                n.AiSummary = result.Summary;
                n.AiTagsJson = JsonSerializer.Serialize(result.Tags);
                n.AiStatus = status;

                foreach (var name in result.Tags)
                {
                    var tag = await db.Tags.FirstOrDefaultAsync(t => t.Name == name, stoppingToken);
                    if (tag is null)
                    {
                        tag = new Tag { Name = name };
                        db.Tags.Add(tag);
                        await db.SaveChangesAsync(stoppingToken);
                    }
                    if (!n.NewsTags.Any(nt => nt.TagId == tag.Id))
                        n.NewsTags.Add(new NewsTag { NewsId = n.Id, TagId = tag.Id });
                }

                await db.SaveChangesAsync(stoppingToken);
                logger.LogInformation("新闻#{NewsId} 摘要生成完成：{Status}，标签[{Tags}]",
                    newsId, status, string.Join(" / ", result.Tags));
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "AI 摘要后台任务异常（新闻#{NewsId}）", newsId);
            }
        }
    }

    private static async Task<AiResult> GenerateWithRetryAsync(IAiSummaryService ai, News n, CancellationToken ct)
    {
        try
        {
            return await ai.GenerateAsync(n.Title, n.Content, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await Task.Delay(1000, ct); // 重试 1 次（计划 8.3）
            return await ai.GenerateAsync(n.Title, n.Content, ct);
        }
    }
}