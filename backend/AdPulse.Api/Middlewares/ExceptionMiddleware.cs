using System.Text.Json;
using AdPulse.Api.Common;
using AdPulse.Api.Dtos;

namespace AdPulse.Api.Middlewares;

/// <summary>全局异常中间件：任何异常都转成统一 JSON 返回体，细节只进日志。</summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (ApiException ex)
        {
            _logger.LogWarning("业务异常 {Code}: {Message}", ex.Code, ex.Message);
            await WriteAsync(ctx, ex.HttpStatus, ApiResponse.Fail(ex.Code, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未处理异常 {Path}", ctx.Request.Path);
            await WriteAsync(ctx, 500, ApiResponse.Fail(50000, "服务器开小差了，请稍后重试"));
        }
    }

    private static async Task WriteAsync(HttpContext ctx, int status, ApiResponse body)
    {
        if (ctx.Response.HasStarted) return;
        ctx.Response.Clear();
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(body, Json));
    }
}