using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace AdPulse.Api.Services;

/// <summary>AI 配置（计划 8.1）：密钥只从配置/环境变量读取，严禁硬编码。</summary>
public class AiOptions
{
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
    // 推理型模型会先消耗“思考”token 再输出正文；预算过小会导致正文被截断（finish_reason=length）
    public int MaxTokens { get; set; } = 2000;
    public int TimeoutSeconds { get; set; } = 60;
    public bool Enabled => !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(ApiKey);
}

public record AiResult(string Summary, List<string> Tags);

public interface IAiSummaryService
{
    bool Enabled { get; }
    Task<AiResult> GenerateAsync(string title, string content, CancellationToken ct);
}

public class AiCallException(string message) : Exception(message);

/// <summary>
/// OpenAI 兼容协议实现：可对接豆包方舟 / DeepSeek / Dify 等任意兼容端点。
/// 提示词要求一句话中文摘要 ≤60 字 + 3~5 个行业标签，严格 JSON，解析做容错清洗。
/// </summary>
public partial class OpenAiSummaryService(
    IHttpClientFactory httpFactory,
    IOptions<AiOptions> options,
    ILogger<OpenAiSummaryService> logger) : IAiSummaryService
{
    private readonly AiOptions _opt = options.Value;
    public bool Enabled => _opt.Enabled;

    public async Task<AiResult> GenerateAsync(string title, string content, CancellationToken ct)
    {
        var client = httpFactory.CreateClient("ai");
        var plain = RuleEngine.StripHtml(content);
        var payload = new
        {
            model = string.IsNullOrWhiteSpace(_opt.Model) ? "default" : _opt.Model,
            max_tokens = _opt.MaxTokens,
            temperature = 0.7,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "你是广告行业新闻编辑。根据新闻标题与正文输出严格 JSON，格式：" +
                              "{\"summary\":\"一句话中文摘要，不超过60字，客观新闻口吻\",\"tags\":[\"3到5个广告行业标签\"]}。" +
                              "不要输出 JSON 以外的解释文字。"
                },
                new { role = "user", content = $"标题：{title}\n正文：{plain[..Math.Min(plain.Length, 2500)]}" }
            }
        };

        var url = _opt.BaseUrl!.TrimEnd('/') + "/chat/completions";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _opt.ApiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var resp = await client.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            logger.LogWarning("AI 端点返回非成功状态 {Status}（{Url}）", (int)resp.StatusCode, url);
            throw new AiCallException($"AI 服务返回 {(int)resp.StatusCode}");
        }

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var choice = doc.RootElement.GetProperty("choices")[0];
        var finish = choice.TryGetProperty("finish_reason", out var fr) ? fr.GetString() : null;
        var raw = choice.GetProperty("message").GetProperty("content").GetString() ?? "";
        if (finish == "length")
            logger.LogWarning("AI 输出因 max_tokens={Max} 被截断，可调大 Ai:MaxTokens", _opt.MaxTokens);
        return ParseResult(raw);
    }

    public const int SummaryMaxLength = 60;

    /// <summary>超长时在 limit 以内最后一个句读处截断，避免半句话；找不到句读再硬截。</summary>
    internal static string TrimToSentence(string text, int limit)
    {
        text = text.Trim();
        if (text.Length <= limit) return text;
        var cut = text[..limit];
        var end = cut.LastIndexOfAny(new[] { '。', '！', '？', '；' });
        if (end >= limit / 2) return cut[..(end + 1)];
        var comma = cut.LastIndexOfAny(new[] { '，', '、', ',' });
        return comma >= limit / 2 ? cut[..comma] + "。" : cut;
    }

    /// <summary>容错解析：剥掉 markdown 代码块包裹，截取首个完整 JSON 对象（计划 8 常见坑）。</summary>
    internal static AiResult ParseResult(string raw)
    {
        var text = Regex.Replace(raw.Trim(), @"^```(?:json)?\s*|\s*```$", "", RegexOptions.Multiline).Trim();
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start) throw new AiCallException("AI 返回内容不含 JSON 对象");

        using var doc = JsonDocument.Parse(text[start..(end + 1)]);
        var summary = doc.RootElement.TryGetProperty("summary", out var s) ? (s.GetString() ?? "").Trim() : "";
        var tags = new List<string>();
        if (doc.RootElement.TryGetProperty("tags", out var t) && t.ValueKind == JsonValueKind.Array)
            foreach (var item in t.EnumerateArray())
            {
                var v = item.GetString()?.Trim();
                if (!string.IsNullOrEmpty(v) && v.Length <= 12 && !tags.Contains(v)) tags.Add(v);
            }

        if (summary.Length == 0) throw new AiCallException("AI 摘要为空");
        summary = TrimToSentence(summary, SummaryMaxLength);
        if (tags.Count == 0) throw new AiCallException("AI 标签为空");
        return new AiResult(summary, tags.Take(5).ToList());
    }
}

/// <summary>规则小编（降级链路）：正文纯文本前两句 ≤80 字 + 行业词库高频标签。</summary>
public static partial class RuleEngine
{
    private static readonly string[] Lexicon =
    {
        "程序化", "信息流", "短视频", "直播", "电商", "品牌", "营销", "广告", "创意", "媒介",
        "投放", "AI", "智能", "数据", "流量", "增长", "消费者", "社交", "搜索", "户外",
        "预算", "内容", "KOL", "CTR", "ROI", "转化", "曝光", "互动", "隐私", "合规",
        "出海", "下沉", "种草", "测评", "代言", "联名", "跨界", "央视", "梯媒", "影院"
    };
    private static readonly string[] PadTags = { "行业动态", "品牌营销", "数字广告" };

    public static string StripHtml(string html)
    {
        var text = Regex.Replace(html, "<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    public static AiResult Generate(string title, string content)
    {
        // 规则摘要：取正文首句，控制在 60 字内并在句读处收尾；正文为空时退回标题
        var plain = StripHtml(content);
        var first = Regex.Split(plain, @"(?<=[。！？!?])").FirstOrDefault(s => s.Trim().Length > 0)?.Trim() ?? "";
        var summary = first.Length == 0 ? title : OpenAiSummaryService.TrimToSentence(first, OpenAiSummaryService.SummaryMaxLength);

        var corpus = title + " " + plain;
        var tags = Lexicon
            .Select(w => (word: w, count: Regex.Matches(corpus, Regex.Escape(w)).Count))
            .Where(x => x.count > 0)
            .OrderByDescending(x => x.count).ThenByDescending(x => x.word.Length)
            .Select(x => x.word).Take(5).ToList();
        foreach (var pad in PadTags)
        {
            if (tags.Count >= 3) break;
            if (!tags.Contains(pad)) tags.Add(pad);
        }
        return new AiResult(summary, tags.Take(5).ToList());
    }
}