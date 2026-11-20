using AdPulse.Api.Data;
using AdPulse.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdPulse.Api.Repositories;

/// <summary>新闻仓储：集中管理新闻相关的复杂查询。</summary>
public class NewsRepository(AppDbContext db)
{
    public IQueryable<News> Query() => db.News.AsQueryable();

    /// <summary>带分类/作者/标签的完整查询。</summary>
    public IQueryable<News> Full() => db.News
        .Include(n => n.Category)
        .Include(n => n.Author)
        .Include(n => n.NewsTags).ThenInclude(nt => nt.Tag);

    public Task<News?> FindFullAsync(int id) => Full().FirstOrDefaultAsync(n => n.Id == id);

    public IQueryable<News> Published() => Full().Where(n => n.Status == NewsStatus.Published);

    public Task<int> CountPublishedAsync() => db.News.CountAsync(n => n.Status == NewsStatus.Published);

    public Task<long> SumViewsAsync() => db.News.Where(n => n.Status == NewsStatus.Published).SumAsync(n => (long)n.ViewCount);
}