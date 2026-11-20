using System.Text.Json;
using AdPulse.Api.Common;
using AdPulse.Api.Data;
using AdPulse.Api.Dtos;
using AdPulse.Api.Models;
using AdPulse.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AdPulse.Api.Services;

public class PortalService(NewsRepository news, IRepository<Category> categories, IRepository<Banner> banners, AppDbContext db)
{
    internal static NewsListItem ToItem(News n) => new(
        n.Id, n.Title, n.Summary, n.CoverUrl, n.CategoryId, n.Category?.Name ?? "",
        n.NewsTags.Select(t => t.Tag!.Name).ToList(), n.ViewCount, n.IsTop, n.PublishedAt);

    internal static List<string> ParseAiTags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(); }
        catch { return new List<string>(); }
    }

    public async Task<HomeData> HomeAsync()
    {
        var bannerList = await banners.Query().Where(b => b.IsEnabled).OrderBy(b => b.SortOrder).ToListAsync();
        var linkIds = bannerList.Where(b => b.LinkNewsId != null).Select(b => b.LinkNewsId!.Value).Distinct().ToList();
        var titles = await news.Query().Where(n => linkIds.Contains(n.Id)).ToDictionaryAsync(n => n.Id, n => n.Title);
        var bannerItems = bannerList.Select(b => new BannerItem(b.Id, b.ImageUrl, b.LinkNewsId,
            b.LinkNewsId != null && titles.TryGetValue(b.LinkNewsId.Value, out var t) ? t : null)).ToList();

        var headlines = (await news.Published().Where(n => n.IsHeadline)
            .OrderByDescending(n => n.PublishedAt).Take(3).ToListAsync()).Select(ToItem).ToList();

        var cats = await categories.Query().OrderBy(c => c.SortOrder).ToListAsync();
        var feeds = new List<CategoryFeed>();
        foreach (var c in cats)
        {
            var items = (await news.Published().Where(n => n.CategoryId == c.Id)
                .OrderByDescending(n => n.PublishedAt).Take(5).ToListAsync()).Select(ToItem).ToList();
            feeds.Add(new CategoryFeed(c.Id, c.Name, items));
        }

        var hot = (await news.Published().OrderByDescending(n => n.ViewCount).Take(10).ToListAsync()).Select(ToItem).ToList();
        var counters = new SiteCounters(await news.CountPublishedAsync(), await news.SumViewsAsync(), cats.Count);
        return new HomeData(bannerItems, headlines, feeds, hot, counters);
    }

    public async Task<PagedResult<NewsListItem>> ListAsync(int? categoryId, string? keyword, int page, int pageSize, string sort)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var q = news.Published();
        if (categoryId is > 0) q = q.Where(n => n.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = $"%{keyword.Trim()}%";
            q = q.Where(n => EF.Functions.Like(n.Title, kw) || EF.Functions.Like(n.Summary, kw));
        }
        var total = await q.CountAsync();
        var ordered = q.OrderByDescending(n => n.IsTop);
        var sorted = sort == "hot" ? ordered.ThenByDescending(n => n.ViewCount) : ordered.ThenByDescending(n => n.PublishedAt);
        var items = (await sorted.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync()).Select(ToItem).ToList();
        return new PagedResult<NewsListItem> { Total = total, Page = page, PageSize = pageSize, Items = items };
    }

    public async Task<NewsDetail> DetailAsync(int id)
    {
        var n = await news.FindFullAsync(id) ?? throw ApiException.NotFound("新闻不存在");
        if (n.Status != NewsStatus.Published) throw ApiException.NotFound("新闻不存在或已下架");

        n.ViewCount++;
        db.ViewLogs.Add(new ViewLog { NewsId = id });
        await db.SaveChangesAsync();

        var prev = await news.Published().Where(x => x.PublishedAt > n.PublishedAt)
            .OrderBy(x => x.PublishedAt).Select(x => new { x.Id, x.Title }).FirstOrDefaultAsync();
        var next = await news.Published().Where(x => x.PublishedAt < n.PublishedAt)
            .OrderByDescending(x => x.PublishedAt).Select(x => new { x.Id, x.Title }).FirstOrDefaultAsync();

        return new NewsDetail(n.Id, n.Title, n.Summary, n.Content, n.CoverUrl, n.CategoryId, n.Category?.Name ?? "",
            n.NewsTags.Select(t => t.Tag!.Name).ToList(), n.ViewCount, n.IsTop, n.PublishedAt, n.Author?.DisplayName ?? "",
            n.AiSummary, n.AiStatus, ParseAiTags(n.AiTagsJson),
            prev?.Id, prev?.Title, next?.Id, next?.Title);
    }

    public async Task<List<RelatedItem>> RelatedAsync(int id)
    {
        var n = await news.FindFullAsync(id) ?? throw ApiException.NotFound("新闻不存在");
        var tagIds = n.NewsTags.Select(t => t.TagId).ToList();
        var list = await news.Published()
            .Where(x => x.Id != id && (x.CategoryId == n.CategoryId || x.NewsTags.Any(nt => tagIds.Contains(nt.TagId))))
            .OrderByDescending(x => x.ViewCount).Take(4)
            .Select(x => new RelatedItem(x.Id, x.Title, x.CoverUrl, x.PublishedAt)).ToListAsync();
        return list;
    }

    public async Task<List<NewsListItem>> HotAsync() =>
        (await news.Published().OrderByDescending(n => n.ViewCount).Take(10).ToListAsync()).Select(ToItem).ToList();

    public async Task<List<CategoryItem>> CategoriesAsync() =>
        await categories.Query().OrderBy(c => c.SortOrder)
            .Select(c => new CategoryItem(c.Id, c.Name, c.Slug, c.SortOrder)).ToListAsync();
}

public class StatsService
{
    private static readonly Lazy<JsonElement> Data = new(() =>
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "industry-stats.json");
        return JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(path));
    });

    public JsonElement Get() => Data.Value;
}