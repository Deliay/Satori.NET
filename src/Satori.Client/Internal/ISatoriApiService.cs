namespace Satori.Client.Internal;

internal interface ISatoriApiService
{
    Task<TData> SendAsync<TData>(string endpoint, string platform, string selfId, object? body,
        CancellationToken cancellationToken = default);

    Task<TData> SendAsync<TData>(string endpoint,
        CancellationToken cancellationToken = default);

    Task<TData> SendAsync<TData>(string endpoint, string platform, string selfId,
        CancellationToken cancellationToken = default);
}