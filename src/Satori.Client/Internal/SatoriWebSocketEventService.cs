using System.Net.WebSockets;
using System.Text.Json;
using Satori.Protocol.Events;
using Websocket.Client;

namespace Satori.Client.Internal;

internal sealed class SatoriWebSocketEventService : ISatoriEventService, IDisposable
{
    private readonly WebsocketClient _ws;
    private readonly Uri _wsUri;
    private readonly string? _token;
    private readonly SatoriClient _satoriClient;
    private CancellationTokenSource? _csc;

    public event EventHandler<Event>? EventReceived;

    private static Func<Uri, CancellationToken, Task<WebSocket>>
        Factory(HttpClient baseClient)
    {
        return Func;

        async Task<WebSocket> Func(Uri wsUri, CancellationToken cancellationToken = default)
        {
            var ws = new ClientWebSocket();
            await ws.ConnectAsync(wsUri, baseClient, cancellationToken);

            return ws;
        }
    }
    
    public SatoriWebSocketEventService(SatoriHttpClient httpClient, SatoriClient satoriClient, string token)
    {
        _wsUri = new Uri(new UriBuilder(httpClient.BaseAddress!) { Scheme = "ws" }.Uri,
            new Uri("/v1/events", UriKind.Relative));
        _ws = new WebsocketClient(_wsUri, logger: null, Factory(httpClient));
        _token = token;
        _satoriClient = satoriClient;

        _ws.MessageReceived.Subscribe(OnMessageReceived);
        _ws.DisconnectionHappened.Subscribe(OnDisconnectionHappened);
        _ws.ReconnectionHappened.Subscribe(OnReconnectionHappened);

    }

    private static readonly Signal PingSignal = new() { Op = SignalOperation.Ping };
    private async Task SignalInterval(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            SendSignal(PingSignal);
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }

    private void SendSignal<T>(T signal) where T : Signal
    {
        var text = JsonSerializer.Serialize(signal, SatoriClient.JsonOptions);
        _satoriClient.Log(LogLevel.Trace, $"WebSocket --Send-> {text}");
        _ws.Send(text);
    }

    private void OnMessageReceived(ResponseMessage message)
    {
        try
        {
            _satoriClient.Log(LogLevel.Trace, $"WebSocket <-Recv-- {message}");
            var json = JsonDocument.Parse(message.Text!);
            var op = (SignalOperation)json.RootElement.GetProperty("op").GetInt32();

            switch (op)
            {
                case SignalOperation.Event:
                    EventReceived?.Invoke(this, json.Deserialize<Signal<Event>>(SatoriClient.JsonOptions)!.Body!);
                    break;
            }
        }
        catch (Exception e)
        {
            _satoriClient.Log(e);
        }
    }

    private void OnDisconnectionHappened(DisconnectionInfo info)
    {
        _satoriClient.Log(LogLevel.Information, $"WebSocket disconnected. Status: {info.CloseStatus}");
    }

    private void OnReconnectionHappened(ReconnectionInfo info)
    {
        if (info.Type == ReconnectionType.Initial)
            return;

        _satoriClient.Log(LogLevel.Information, "WebSocket reconnecting...");
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _csc = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var newCancelToken = _csc.Token;
        
        _satoriClient.Log(LogLevel.Debug, $"Connecting to {_wsUri}...");
        await _ws.StartOrFail().WaitAsync(newCancelToken);
        
        var identify = new Signal<IdentifySignalBody>
        {
            Op = SignalOperation.Identify,
            Body = new IdentifySignalBody { Token = _token }
        };
        SendSignal(identify);
        
        _ = SignalInterval(newCancelToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        using var cancelSource = _csc;
        
        await _ws.StopOrFail(WebSocketCloseStatus.NormalClosure, "")
            .WaitAsync(cancellationToken);
    }

    public void Dispose()
    {
        _ws.Dispose();
        _csc?.Dispose();
    }
}