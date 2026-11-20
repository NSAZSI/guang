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
    public int MaxTokens { get; set; } = 300;
    public int TimeoutSeconds { get; set; } = 30;
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
        var raw = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        return ParseResult(raw);
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
        if (summary.Length > 60) summary = summary[..60];
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
        var plain = StripHtml(content);
        var sentences = Regex.Split(plain, @"(?<=[。！？!?])")
            .Where(s => s.Trim().Length > 0).Take(2).ToArray();
        var summary = string.Join("", sentences).Trim();
        if (summary.Length == 0) summary = title;
        if (summary.Length > 80) summary = summary[..80] + "…";

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