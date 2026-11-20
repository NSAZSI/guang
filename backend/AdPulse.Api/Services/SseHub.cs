using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Channels;

namespace AdPulse.Api.Services;

/// <summary>
/// SSE 广播中心（单例）：每个连接一个 Channel，发布新闻时向所有在线页面推送。
/// </summary>
public class SseHub
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly ConcurrentDictionary<Guid, Channel<string>> _clients = new();

    public int ClientCount => _clients.Count;

    public (Guid Id, ChannelReader<string> Reader) Subscribe()
    {
        var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
        var id = Guid.NewGuid();
        _clients[id] = channel;
        return (id, channel.Reader);
    }

    public void Unsubscribe(Guid id)
    {
        if (_clients.TryRemove(id, out var channel)) channel.Writer.TryComplete();
    }

    public void Broadcast(string eventName, object payload)
    {
        var frame = $"event: {eventName}\ndata: {JsonSerializer.Serialize(payload, Json)}\n\n";
        foreach (var channel in _clients.Values) channel.Writer.TryWrite(frame);
    }
}