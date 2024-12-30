using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Satori.Client.Internal;

internal class SatoriHttpApiService : ISatoriApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<SatoriHttpClient>? _logger;

    internal SatoriHttpApiService(SatoriHttpClient client, ILogger<SatoriHttpClient>? logger = null)
    {
        _http = client;
        _logger = logger;
    }

    public Task<TData> SendAsync<TData>(string endpoint,
        CancellationToken cancellationToken = default) =>
        SendAsync<TData>(endpoint, "", "", null, cancellationToken);

    public Task<TData> SendAsync<TData>(string endpoint, string platform, string selfId,
        CancellationToken cancellationToken = default) =>
        SendAsync<TData>(endpoint, platform, selfId, null, cancellationToken);
    
    public async Task<TData> SendAsync<TData>(string endpoint, string platform, string selfId, object? body,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("X-Platform", platform);
        request.Headers.Add("X-Self-ID", selfId);
        
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        var response = await _http.SendAsync(request, cancellationToken);

        try
        {
            var data = await response.Content.ReadFromJsonAsync<TData>(SatoriClient.JsonOptions,
                cancellationToken: cancellationToken);
            return data!;
        }
        catch (Exception)
        {
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"Error caught, response code ={response.StatusCode}, response content = {raw}");
            throw;
        }
    }
}