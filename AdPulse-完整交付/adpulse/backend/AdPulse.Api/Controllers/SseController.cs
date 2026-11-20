using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Channels;
using AdPulse.Api.Dtos;
using AdPulse.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdPulse.Api.Controllers;

[ApiController]
[Route("api/sse")]
[AllowAnonymous]
public class SseController(SseHub hub) : ControllerBase
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>实时快讯通道：text/event-stream，15 秒心跳，断线由浏览器 EventSource 自动重连。</summary>
    [HttpGet("newsflash")]
    public async Task NewsFlash(CancellationToken ct)
    {
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no"; // 提示 Nginx 关闭缓冲

        var (id, reader) = hub.Subscribe();
        try
        {
            await WriteEventAsync("hello", new { message = "connected", clients = hub.ClientCount }, ct);
            using var heartbeat = new PeriodicTimer(TimeSpan.FromSeconds(15));
            var readTask = reader.ReadAsync(ct).AsTask();
            while (!ct.IsCancellationRequested)
            {
                var tickTask = heartbeat.WaitForNextTickAsync(ct).AsTask();
                var done = await Task.WhenAny(readTask, tickTask);
                if (done == readTask)
                {
                    var frame = await readTask;
                    await Response.WriteAsync(frame, ct);
                    await Response.Body.FlushAsync(ct);
                    readTask = reader.ReadAsync(ct).AsTask();
                }
                else
                {
                    await tickTask;
                    await Response.WriteAsync(": heartbeat\n\n", ct);
                    await WriteEventAsync("ping", new { t = DateTimeOffset.UtcNow.ToUnixTimeSeconds() }, ct);
                }
            }
        }
        catch (OperationCanceledException) { /* 客户端断开 */ }
        catch (ChannelClosedException) { /* 服务端主动关闭 */ }
        finally
        {
            hub.Unsubscribe(id);
        }
    }

    /// <summary>在线连接数（验收与运维观察用）。</summary>
    [HttpGet("stats")]
    public ApiResponse Stats() => ApiResponse.Ok(new { clients = hub.ClientCount });

    private async Task WriteEventAsync(string name, object data, CancellationToken ct)
    {
        await Response.WriteAsync($"event: {name}\ndata: {JsonSerializer.Serialize(data, Json)}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}