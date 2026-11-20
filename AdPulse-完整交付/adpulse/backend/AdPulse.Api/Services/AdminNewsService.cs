using System.Security.Claims;
using AdPulse.Api.Common;
using AdPulse.Api.Data;
using AdPulse.Api.Dtos;
using AdPulse.Api.Models;
using AdPulse.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AdPulse.Api.Services;

public class AdminNewsService(NewsRepository news, IRepository<Tag> tags, AppDbContext db, SseHub hub, AiTaskQueue aiQueue)
{
    private void BroadcastPublished(News n) =>
        hub.Broadcast("news", new { id = n.Id, title = n.Title, time = DateTime.UtcNow });
    private void EnsureOwnership(News n, ClaimsPrincipal user)
    {
        if (!user.IsAdmin() && n.AuthorId != user.UserId())
            throw ApiException.Forbidden("只能操作自己的文章");
    }

    private void Log(ClaimsPrincipal user, string action, int newsId, string detail) =>
        db.OperationLogs.Add(new OperationLog
        {
            UserId = user.UserId(), UserName = user.UserName(),
            Action = action, Target = $"新闻#{newsId}", Detail = detail
        });

    public async Task<PagedResult<AdminNewsListItem>> PagedAsync(ClaimsPrincipal user, string? status, int? categoryId, string? keyword, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var q = news.Full();
        if (!user.IsAdmin()) q = q.Where(n => n.AuthorId == user.UserId());

        if (status == NewsStatus.Recycled)
        {
            if (!user.IsAdmin()) throw ApiException.Forbidden("回收站仅管理员可见");
            q = q.Where(n => n.Status == NewsStatus.Recycled);
        }
        else if (!string.IsNullOrWhiteSpace(status)) q = q.Where(n => n.Status == status);
        else q = q.Where(n => n.Status != NewsStatus.Recycled);

        if (categoryId is > 0) q = q.Where(n => n.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = $"%{keyword.Trim()}%";
            q = q.Where(n => EF.Functions.Like(n.Title, kw));
        }

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(n => n.UpdatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(n => new AdminNewsListItem(n.Id, n.Title, n.CoverUrl, n.CategoryId, n.Category!.Name,
                n.Status, n.IsTop, n.IsHeadline, n.ViewCount, n.AuthorId, n.Author!.DisplayName,
                n.AiStatus, n.PublishedAt, n.UpdatedAt)).ToListAsync();
        return new PagedResult<AdminNewsListItem> { Total = total, Page = page, PageSize = pageSize, Items = items };
    }

    public async Task<AdminNewsDetail> DetailAsync(ClaimsPrincipal user, int id)
    {
        var n = await news.FindFullAsync(id) ?? throw ApiException.NotFound("新闻不存在");
        EnsureOwnership(n, user);
        return new AdminNewsDetail(n.Id, n.Title, n.Summary, n.Content, n.CoverUrl, n.CategoryId,
            n.NewsTags.Select(t => t.TagId).ToList(), n.Status, n.IsTop, n.IsHeadline, n.AiStatus, n.AiSummary);
    }

    private async Task ValidateTaxonomyAsync(SaveNewsRequest req)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == req.CategoryId))
            throw ApiException.BadRequest("所选分类不存在");
        var tagIds = req.TagIds.Distinct().ToList();
        if (tagIds.Count > 0 && await tags.Query().CountAsync(t => tagIds.Contains(t.Id)) != tagIds.Count)
            throw ApiException.BadRequest("包含不存在的标签");
    }

    public async Task<int> CreateAsync(ClaimsPrincipal user, SaveNewsRequest req)
    {
        await ValidateTaxonomyAsync(req);
        var now = DateTime.UtcNow;
        var n = new News
        {
            Title = req.Title.Trim(), Summary = req.Summary.Trim(), Content = req.Content, CoverUrl = req.CoverUrl,
            CategoryId = req.CategoryId, AuthorId = user.UserId(),
            Status = req.Publish ? NewsStatus.Published : NewsStatus.Draft,
            PublishedAt = req.Publish ? now : null,
            IsTop = req.IsTop, IsHeadline = req.IsHeadline,
            AiStatus = req.Publish ? AiStatus.Pending : AiStatus.None,
            CreatedAt = now, UpdatedAt = now
        };
        foreach (var t in req.TagIds.Distinct()) n.NewsTags.Add(new NewsTag { TagId = t });
        db.News.Add(n);
        Log(user, req.Publish ? "新建并发布新闻" : "新建草稿", n.Id, n.Title);
        await db.SaveChangesAsync();
        if (req.Publish)
        {
            BroadcastPublished(n);
            aiQueue.Enqueue(n.Id); // AI 摘要异步生成，不阻塞发布（计划 8.2）
        }
        return n.Id;
    }

    public async Task UpdateAsync(ClaimsPrincipal user, int id, SaveNewsRequest req)
    {
        await ValidateTaxonomyAsync(req);
        var n = await news.FindFullAsync(id) ?? throw ApiException.NotFound("新闻不存在");
        EnsureOwnership(n, user);

        n.Title = req.Title.Trim(); n.Summary = req.Summary.Trim(); n.Content = req.Content; n.CoverUrl = req.CoverUrl;
        n.CategoryId = req.CategoryId; n.IsTop = req.IsTop; n.IsHeadline = req.IsHeadline; n.UpdatedAt = DateTime.UtcNow;

        var target = req.Publish ? NewsStatus.Published : NewsStatus.Draft;
        var wasPublished = n.Status == NewsStatus.Published;
        if (target == NewsStatus.Published && n.Status != NewsStatus.Published) n.PublishedAt = DateTime.UtcNow;
        if (target == NewsStatus.Draft) n.PublishedAt = null;
        if (target == NewsStatus.Published && !wasPublished) n.AiStatus = AiStatus.Pending;
        n.Status = target;

        db.NewsTags.RemoveRange(n.NewsTags);
        n.NewsTags.Clear();
        foreach (var t in req.TagIds.Distinct()) n.NewsTags.Add(new NewsTag { NewsId = id, TagId = t });

        Log(user, req.Publish ? "编辑并发布新闻" : "编辑草稿", id, n.Title);
        await db.SaveChangesAsync();
        if (target == NewsStatus.Published && !wasPublished)
        {
            BroadcastPublished(n);
            aiQueue.Enqueue(id);
        }
    }

    public async Task ChangeStatusAsync(ClaimsPrincipal user, int id, string action)
    {
        var n = await news.Query().FirstOrDefaultAsync(x => x.Id == id) ?? throw ApiException.NotFound("新闻不存在");
        switch (action)
        {
            case "publish":
                EnsureOwnership(n, user);
                if (n.Status == NewsStatus.Published) throw ApiException.BadRequest("该新闻已是发布状态");
                if (n.Status == NewsStatus.Recycled) throw ApiException.BadRequest("请先从回收站还原");
                if (string.IsNullOrWhiteSpace(n.Summary) || string.IsNullOrWhiteSpace(n.CoverUrl))
                    throw ApiException.BadRequest("发布前请先补全摘要与封面图");
                n.Status = NewsStatus.Published; n.PublishedAt = DateTime.UtcNow;
                n.AiStatus = AiStatus.Pending;
                break;
            case "unpublish":
                EnsureOwnership(n, user);
                if (n.Status != NewsStatus.Published) throw ApiException.BadRequest("仅已发布新闻可撤回");
                n.Status = NewsStatus.Draft; n.PublishedAt = null; n.IsTop = false;
                break;
            case "top":
            case "untop":
                EnsureOwnership(n, user);
                if (n.Status != NewsStatus.Published) throw ApiException.BadRequest("仅已发布新闻可置顶");
                n.IsTop = action == "top";
                break;
            case "recycle":
                EnsureOwnership(n, user);
                if (n.Status == NewsStatus.Recycled) throw ApiException.BadRequest("该新闻已在回收站");
                n.Status = NewsStatus.Recycled; n.IsTop = false; n.IsHeadline = false;
                break;
            case "restore":
                if (!user.IsAdmin()) throw ApiException.Forbidden("回收站还原仅管理员可操作");
                if (n.Status != NewsStatus.Recycled) throw ApiException.BadRequest("仅回收站新闻可还原");
                n.Status = NewsStatus.Draft;
                break;
        }
        n.UpdatedAt = DateTime.UtcNow;
        var labels = new Dictionary<string, string>
        {
            ["publish"] = "发布新闻", ["unpublish"] = "撤回新闻", ["top"] = "置顶新闻",
            ["untop"] = "取消置顶", ["recycle"] = "移入回收站", ["restore"] = "还原新闻"
        };
        Log(user, labels.GetValueOrDefault(action, action), id, n.Title);
        await db.SaveChangesAsync();
        if (action == "publish")
        {
            BroadcastPublished(n);
            aiQueue.Enqueue(id);
        }
    }

    public Task DeleteAsync(ClaimsPrincipal user, int id) => ChangeStatusAsync(user, id, "recycle");

    /// <summary>手动重新生成 AI 摘要（计划 8.2）：仅作者或 Admin；草稿与已发布均可；立即返回 Pending，后台队列异步完成。</summary>
    public async Task<object> RegenAiAsync(ClaimsPrincipal user, int id)
    {
        var n = await news.Query().FirstOrDefaultAsync(x => x.Id == id) ?? throw ApiException.NotFound("新闻不存在");
        EnsureOwnership(n, user);
        if (n.Status == NewsStatus.Recycled) throw ApiException.BadRequest("回收站中的新闻请先还原再生成 AI 摘要");
        n.AiStatus = AiStatus.Pending;
        n.UpdatedAt = DateTime.UtcNow;
        Log(user, "重新生成AI摘要", id, n.Title);
        await db.SaveChangesAsync();
        aiQueue.Enqueue(id);
        return new { aiStatus = AiStatus.Pending };
    }
}