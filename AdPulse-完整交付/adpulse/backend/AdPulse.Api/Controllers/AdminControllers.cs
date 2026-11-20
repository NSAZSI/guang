using AdPulse.Api.Dtos;
using AdPulse.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdPulse.Api.Controllers;

[ApiController]
[Route("api/admin/news")]
[Authorize]
public class AdminNewsController(AdminNewsService svc) : ControllerBase
{
    [HttpGet]
    public async Task<ApiResponse> List(string? status, int? categoryId, string? keyword, int page = 1, int pageSize = 10)
        => ApiResponse.Ok(await svc.PagedAsync(User, status, categoryId, keyword, page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<ApiResponse> Detail(int id) => ApiResponse.Ok(await svc.DetailAsync(User, id));

    [HttpPost]
    public async Task<ApiResponse> Create(SaveNewsRequest req) => ApiResponse.Ok(new { id = await svc.CreateAsync(User, req) });

    [HttpPut("{id:int}")]
    public async Task<ApiResponse> Update(int id, SaveNewsRequest req)
    {
        await svc.UpdateAsync(User, id, req);
        return ApiResponse.Ok();
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ApiResponse> ChangeStatus(int id, ChangeStatusRequest req)
    {
        await svc.ChangeStatusAsync(User, id, req.Action);
        return ApiResponse.Ok();
    }

    [HttpDelete("{id:int}")]
    public async Task<ApiResponse> Delete(int id)
    {
        await svc.DeleteAsync(User, id);
        return ApiResponse.Ok();
    }

    /// <summary>AI 摘要手动重新生成（步骤 6）：异步队列处理，立即返回 Pending。</summary>
    [HttpPost("{id:int}/ai-summary")]
    public async Task<ApiResponse> AiSummary(int id) => ApiResponse.Ok(await svc.RegenAiAsync(User, id));
}

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminTaxonomyController(TaxonomyService svc) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<ApiResponse> Categories() => ApiResponse.Ok(await svc.ListCategoriesAsync());

    [HttpPost("categories")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse> CreateCategory(SaveCategoryRequest req) => ApiResponse.Ok(new { id = await svc.CreateCategoryAsync(User, req) });

    [HttpPut("categories/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse> UpdateCategory(int id, SaveCategoryRequest req) { await svc.UpdateCategoryAsync(User, id, req); return ApiResponse.Ok(); }

    [HttpDelete("categories/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse> DeleteCategory(int id) { await svc.DeleteCategoryAsync(User, id); return ApiResponse.Ok(); }

    [HttpGet("tags")]
    public async Task<ApiResponse> Tags() => ApiResponse.Ok(await svc.ListTagsAsync());

    [HttpPost("tags")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse> CreateTag(SaveTagRequest req) => ApiResponse.Ok(new { id = await svc.CreateTagAsync(User, req) });

    [HttpPut("tags/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse> UpdateTag(int id, SaveTagRequest req) { await svc.UpdateTagAsync(User, id, req); return ApiResponse.Ok(); }

    [HttpDelete("tags/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse> DeleteTag(int id) { await svc.DeleteTagAsync(User, id); return ApiResponse.Ok(); }
}

[ApiController]
[Route("api/admin/banners")]
[Authorize]
public class AdminBannersController(BannerService svc) : ControllerBase
{
    [HttpGet]
    public async Task<ApiResponse> List() => ApiResponse.Ok(await svc.ListAsync());

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse> Create(SaveBannerRequest req) => ApiResponse.Ok(new { id = await svc.CreateAsync(User, req) });

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse> Update(int id, SaveBannerRequest req) { await svc.UpdateAsync(User, id, req); return ApiResponse.Ok(); }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse> Delete(int id) { await svc.DeleteAsync(User, id); return ApiResponse.Ok(); }
}

[ApiController]
[Route("api/admin/upload")]
[Authorize]
public class AdminUploadController(UploadService svc) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ApiResponse> Upload(IFormFile? file) => ApiResponse.Ok(await svc.SaveAsync(file));
}

[ApiController]
[Route("api/admin/dashboard")]
[Authorize]
public class AdminDashboardController(DashboardService svc) : ControllerBase
{
    [HttpGet]
    public async Task<ApiResponse> Get() => ApiResponse.Ok(await svc.GetAsync());
}

[ApiController]
[Route("api/admin/logs")]
[Authorize(Policy = "AdminOnly")]
public class AdminLogsController(LogService svc) : ControllerBase
{
    [HttpGet]
    public async Task<ApiResponse> List(int page = 1, int pageSize = 10) => ApiResponse.Ok(await svc.PagedAsync(page, pageSize));
}