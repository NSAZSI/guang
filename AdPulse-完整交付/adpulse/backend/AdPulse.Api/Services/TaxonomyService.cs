using System.Security.Claims;
using AdPulse.Api.Common;
using AdPulse.Api.Data;
using AdPulse.Api.Dtos;
using AdPulse.Api.Models;
using AdPulse.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AdPulse.Api.Services;

public class TaxonomyService(IRepository<Category> categories, IRepository<Tag> tags, AppDbContext db)
{
    public Task<List<CategoryItem>> ListCategoriesAsync() =>
        categories.Query().OrderBy(c => c.SortOrder)
            .Select(c => new CategoryItem(c.Id, c.Name, c.Slug, c.SortOrder)).ToListAsync();

    public async Task<int> CreateCategoryAsync(ClaimsPrincipal user, SaveCategoryRequest req)
    {
        if (await categories.Query().AnyAsync(c => c.Name == req.Name)) throw ApiException.BadRequest("分类名已存在");
        var c = new Category { Name = req.Name.Trim(), Slug = req.Slug.Trim(), SortOrder = req.SortOrder };
        await categories.AddAsync(c);
        db.OperationLogs.Add(new OperationLog { UserId = user.UserId(), UserName = user.UserName(), Action = "新建分类", Target = $"分类#{c.Id}", Detail = c.Name });
        await categories.SaveAsync();
        return c.Id;
    }

    public async Task UpdateCategoryAsync(ClaimsPrincipal user, int id, SaveCategoryRequest req)
    {
        var c = await categories.GetAsync(id) ?? throw ApiException.NotFound("分类不存在");
        if (await categories.Query().AnyAsync(x => x.Name == req.Name && x.Id != id)) throw ApiException.BadRequest("分类名已存在");
        c.Name = req.Name.Trim(); c.Slug = req.Slug.Trim(); c.SortOrder = req.SortOrder;
        db.OperationLogs.Add(new OperationLog { UserId = user.UserId(), UserName = user.UserName(), Action = "编辑分类", Target = $"分类#{id}", Detail = c.Name });
        await categories.SaveAsync();
    }

    public async Task DeleteCategoryAsync(ClaimsPrincipal user, int id)
    {
        var c = await categories.GetAsync(id) ?? throw ApiException.NotFound("分类不存在");
        if (await db.News.AnyAsync(n => n.CategoryId == id)) throw ApiException.BadRequest("该分类下仍有新闻，无法删除");
        db.OperationLogs.Add(new OperationLog { UserId = user.UserId(), UserName = user.UserName(), Action = "删除分类", Target = $"分类#{id}", Detail = c.Name });
        categories.Remove(c);
        await categories.SaveAsync();
    }

    public async Task<List<object>> ListTagsAsync() =>
        await tags.Query().OrderBy(t => t.Id)
            .Select(t => new { t.Id, t.Name, Usage = t.NewsTags.Count }).ToListAsync<object>();

    public async Task<int> CreateTagAsync(ClaimsPrincipal user, SaveTagRequest req)
    {
        if (await tags.Query().AnyAsync(t => t.Name == req.Name)) throw ApiException.BadRequest("标签名已存在");
        var t = new Tag { Name = req.Name.Trim() };
        await tags.AddAsync(t);
        db.OperationLogs.Add(new OperationLog { UserId = user.UserId(), UserName = user.UserName(), Action = "新建标签", Target = $"标签#{t.Id}", Detail = t.Name });
        await tags.SaveAsync();
        return t.Id;
    }

    public async Task UpdateTagAsync(ClaimsPrincipal user, int id, SaveTagRequest req)
    {
        var t = await tags.GetAsync(id) ?? throw ApiException.NotFound("标签不存在");
        if (await tags.Query().AnyAsync(x => x.Name == req.Name && x.Id != id)) throw ApiException.BadRequest("标签名已存在");
        t.Name = req.Name.Trim();
        db.OperationLogs.Add(new OperationLog { UserId = user.UserId(), UserName = user.UserName(), Action = "编辑标签", Target = $"标签#{id}", Detail = t.Name });
        await tags.SaveAsync();
    }

    public async Task DeleteTagAsync(ClaimsPrincipal user, int id)
    {
        var t = await tags.GetAsync(id) ?? throw ApiException.NotFound("标签不存在");
        db.OperationLogs.Add(new OperationLog { UserId = user.UserId(), UserName = user.UserName(), Action = "删除标签", Target = $"标签#{id}", Detail = t.Name });
        tags.Remove(t);
        await tags.SaveAsync();
    }
}

public class BannerService(IRepository<Banner> banners, AppDbContext db)
{
    public async Task<List<object>> ListAsync() =>
        await banners.Query().OrderBy(b => b.SortOrder)
            .Select(b => new { b.Id, b.ImageUrl, b.LinkNewsId, LinkNewsTitle = b.LinkNews != null ? b.LinkNews.Title : null, b.SortOrder, b.IsEnabled })
            .ToListAsync<object>();

    private async Task ValidateAsync(SaveBannerRequest req)
    {
        if (req.LinkNewsId is > 0 && !await db.News.AnyAsync(n => n.Id == req.LinkNewsId))
            throw ApiException.BadRequest("关联的新闻不存在");
    }

    public async Task<int> CreateAsync(ClaimsPrincipal user, SaveBannerRequest req)
    {
        await ValidateAsync(req);
        var b = new Banner { ImageUrl = req.ImageUrl, LinkNewsId = req.LinkNewsId, SortOrder = req.SortOrder, IsEnabled = req.IsEnabled };
        await banners.AddAsync(b);
        db.OperationLogs.Add(new OperationLog { UserId = user.UserId(), UserName = user.UserName(), Action = "新建轮播图", Target = $"轮播#{b.Id}", Detail = req.ImageUrl });
        await banners.SaveAsync();
        return b.Id;
    }

    public async Task UpdateAsync(ClaimsPrincipal user, int id, SaveBannerRequest req)
    {
        await ValidateAsync(req);
        var b = await banners.GetAsync(id) ?? throw ApiException.NotFound("轮播图不存在");
        b.ImageUrl = req.ImageUrl; b.LinkNewsId = req.LinkNewsId; b.SortOrder = req.SortOrder; b.IsEnabled = req.IsEnabled;
        db.OperationLogs.Add(new OperationLog { UserId = user.UserId(), UserName = user.UserName(), Action = "编辑轮播图", Target = $"轮播#{id}", Detail = req.ImageUrl });
        await banners.SaveAsync();
    }

    public async Task DeleteAsync(ClaimsPrincipal user, int id)
    {
        var b = await banners.GetAsync(id) ?? throw ApiException.NotFound("轮播图不存在");
        db.OperationLogs.Add(new OperationLog { UserId = user.UserId(), UserName = user.UserName(), Action = "删除轮播图", Target = $"轮播#{id}", Detail = b.ImageUrl });
        banners.Remove(b);
        await banners.SaveAsync();
    }
}