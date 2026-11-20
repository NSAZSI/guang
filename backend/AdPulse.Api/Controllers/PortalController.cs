using AdPulse.Api.Dtos;
using AdPulse.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdPulse.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ApiResponse> Login(LoginRequest req) => ApiResponse.Ok(await auth.LoginAsync(req));

    [HttpGet("me")]
    [Authorize]
    public async Task<ApiResponse> Me() => ApiResponse.Ok(await auth.MeAsync(User));
}

[ApiController]
[Route("api/portal")]
[AllowAnonymous]
public class PortalController(PortalService portal, StatsService stats) : ControllerBase
{
    [HttpGet("home")]
    public async Task<ApiResponse> Home() => ApiResponse.Ok(await portal.HomeAsync());

    [HttpGet("news")]
    public async Task<ApiResponse> News(int? categoryId, string? keyword, int page = 1, int pageSize = 10, string sort = "time")
        => ApiResponse.Ok(await portal.ListAsync(categoryId, keyword, page, pageSize, sort));

    [HttpGet("news/{id:int}")]
    public async Task<ApiResponse> Detail(int id) => ApiResponse.Ok(await portal.DetailAsync(id));

    [HttpGet("news/{id:int}/related")]
    public async Task<ApiResponse> Related(int id) => ApiResponse.Ok(await portal.RelatedAsync(id));

    [HttpGet("hot")]
    public async Task<ApiResponse> Hot() => ApiResponse.Ok(await portal.HotAsync());

    [HttpGet("categories")]
    public async Task<ApiResponse> Categories() => ApiResponse.Ok(await portal.CategoriesAsync());

    [HttpGet("stats")]
    public ApiResponse Stats() => ApiResponse.Ok(stats.Get());
}