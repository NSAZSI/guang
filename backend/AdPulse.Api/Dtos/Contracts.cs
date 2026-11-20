namespace AdPulse.Api.Dtos;

/// <summary>全站统一返回体（计划 2.3）。</summary>
public class ApiResponse
{
    public int Code { get; set; }
    public string Message { get; set; } = "ok";
    public object? Data { get; set; }

    public static ApiResponse Ok(object? data = null) => new() { Code = 0, Message = "ok", Data = data };
    public static ApiResponse Fail(int code, string message) => new() { Code = code, Message = message, Data = null };
}

public class PagedResult<T>
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<T> Items { get; set; } = new();
}

// ---------- 认证 ----------
public record LoginRequest(string UserName, string Password);
public record LoginResponse(string Token, DateTime ExpiresAt, UserInfo User);
public record UserInfo(int Id, string UserName, string DisplayName, string Role);

// ---------- 前台 ----------
public record NewsListItem(int Id, string Title, string Summary, string CoverUrl, int CategoryId, string CategoryName,
    List<string> Tags, int ViewCount, bool IsTop, DateTime? PublishedAt);

public record NewsDetail(int Id, string Title, string Summary, string Content, string CoverUrl, int CategoryId,
    string CategoryName, List<string> Tags, int ViewCount, bool IsTop, DateTime? PublishedAt, string AuthorName,
    string? AiSummary, string AiStatus, List<string> AiTags,
    int? PrevId, string? PrevTitle, int? NextId, string? NextTitle);

public record HomeData(List<BannerItem> Banners, List<NewsListItem> Headlines, List<CategoryFeed> CategoryFeeds,
    List<NewsListItem> Hot, SiteCounters Counters);

public record BannerItem(int Id, string ImageUrl, int? LinkNewsId, string? LinkNewsTitle);
public record CategoryFeed(int CategoryId, string CategoryName, List<NewsListItem> Items);
public record SiteCounters(int NewsCount, long TotalViews, int CategoryCount);
public record CategoryItem(int Id, string Name, string Slug, int SortOrder);
public record RelatedItem(int Id, string Title, string CoverUrl, DateTime? PublishedAt);

// ---------- 后台 ----------
public record AdminNewsListItem(int Id, string Title, string CoverUrl, int CategoryId, string CategoryName,
    string Status, bool IsTop, bool IsHeadline, int ViewCount, int AuthorId, string AuthorName,
    string AiStatus, DateTime? PublishedAt, DateTime UpdatedAt);

public record AdminNewsDetail(int Id, string Title, string Summary, string Content, string CoverUrl, int CategoryId,
    List<int> TagIds, string Status, bool IsTop, bool IsHeadline, string AiStatus, string? AiSummary);

public record SaveNewsRequest(string Title, string Summary, string Content, string CoverUrl, int CategoryId,
    List<int> TagIds, bool Publish, bool IsTop, bool IsHeadline);

public record ChangeStatusRequest(string Action); // publish/unpublish/top/untop/recycle/restore

public record SaveCategoryRequest(string Name, string Slug, int SortOrder);
public record SaveTagRequest(string Name);
public record SaveBannerRequest(string ImageUrl, int? LinkNewsId, int SortOrder, bool IsEnabled);

public record DashboardData(List<TrendPoint> PublishTrend, List<TrendPoint> ViewTrend,
    List<NameValue> CategoryShare, List<NewsListItem> Top10);
public record TrendPoint(string Date, int Count);
public record NameValue(string Name, int Value);
public record OperationLogItem(int Id, string UserName, string Action, string Target, string Detail, DateTime CreatedAt);