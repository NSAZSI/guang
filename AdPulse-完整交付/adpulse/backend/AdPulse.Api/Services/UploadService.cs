using AdPulse.Api.Common;
using AdPulse.Api.Data;
using AdPulse.Api.Dtos;
using AdPulse.Api.Models;
using AdPulse.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AdPulse.Api.Services;

public class UploadService(IWebHostEnvironment env, IConfiguration config)
{
    private static readonly string[] AllowedExt = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxSize = 5 * 1024 * 1024;

    public async Task<object> SaveAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0) throw ApiException.BadRequest("请选择要上传的图片");
        if (file.Length > MaxSize) throw ApiException.BadRequest("图片不能超过 5MB");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExt.Contains(ext)) throw ApiException.BadRequest("仅支持 jpg / png / webp 格式");
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw ApiException.BadRequest("文件内容类型必须是图片");

        var uploadPath = config["UploadPath"];
        if (string.IsNullOrWhiteSpace(uploadPath))
            uploadPath = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadPath);

        var name = Guid.NewGuid().ToString("N") + ext;
        await using var fs = File.Create(Path.Combine(uploadPath, name));
        await file.CopyToAsync(fs);
        return new { url = $"/uploads/{name}" };
    }
}

public class DashboardService(AppDbContext db, NewsRepository news)
{
    public async Task<DashboardData> GetAsync()
    {
        var today = DateTime.UtcNow.Date;
        var days = Enumerable.Range(0, 30).Select(i => today.AddDays(-29 + i)).ToList();
        var start = days.First();

        var publishRaw = await db.News
            .Where(n => n.PublishedAt != null && n.PublishedAt >= start && n.Status == NewsStatus.Published)
            .GroupBy(n => n.PublishedAt!.Value.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() }).ToListAsync();
        var viewRaw = await db.ViewLogs.Where(v => v.ViewedAt >= start)
            .GroupBy(v => v.ViewedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() }).ToListAsync();

        List<TrendPoint> Fill(List<(DateTime Date, int Count)> raw) =>
            days.Select(d => new TrendPoint(d.ToString("yyyy-MM-dd"), raw.FirstOrDefault(r => r.Date == d).Count)).ToList();

        var share = await db.News.Where(n => n.Status == NewsStatus.Published)
            .GroupBy(n => n.Category!.Name)
            .Select(g => new NameValue(g.Key, g.Count())).ToListAsync();

        var top10 = (await news.Published().OrderByDescending(n => n.ViewCount).Take(10).ToListAsync())
            .Select(PortalService.ToItem).ToList();

        return new DashboardData(
            Fill(publishRaw.Select(x => (x.Date, x.Count)).ToList()),
            Fill(viewRaw.Select(x => (x.Date, x.Count)).ToList()),
            share, top10);
    }
}

public class LogService(IRepository<OperationLog> logs)
{
    public async Task<PagedResult<OperationLogItem>> PagedAsync(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var q = logs.Query().OrderByDescending(l => l.Id);
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new OperationLogItem(l.Id, l.UserName, l.Action, l.Target, l.Detail, l.CreatedAt)).ToListAsync();
        return new PagedResult<OperationLogItem> { Total = total, Page = page, PageSize = pageSize, Items = items };
    }
}