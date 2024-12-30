using System.Runtime.CompilerServices;
using System.Text.Json;
using Satori.Client.Internal;
using Satori.Protocol.Models;

namespace Satori.Client;

public class SatoriClient : IDisposable
{
    internal readonly ISatoriApiService ApiService;
    internal readonly ISatoriEventService EventService;

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public event EventHandler<SatoriClientLog>? Logging;

    public SatoriClient(string baseUri, string? token = null) : this(new Uri(baseUri), token)
    {
    }

    public SatoriClient(Uri baseUri, string? token = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);
        
        var client = new SatoriHttpClient(baseUri, token);
        ApiService = new SatoriHttpApiService(client);
        EventService = new SatoriWebSocketEventService(client, this, token);
    }

    internal void Log(LogLevel logLevel, string message) =>
        Logging?.Invoke(this, new SatoriClientLog(logLevel, message));

    internal void Log(Exception e) => Logging?.Invoke(this, new SatoriClientLog(e));

    public Task<Login> GetLoginAsync(CancellationToken cancellationToken = default)
    {
        return ApiService.SendAsync<Login>("/v1/login.get", cancellationToken);
    }
    
    public SatoriBot Bot(string platform, string selfId)
    {
        return new SatoriBot(this, platform, selfId);
    }

    public Task StartAsync() => EventService.StartAsync();

    public Task StopAsync() => EventService.StopAsync();

    public void Dispose()
    {
        if (EventService is IDisposable eventDisposable)
            eventDisposable.Dispose();

        GC.SuppressFinalize(this);
    }
}