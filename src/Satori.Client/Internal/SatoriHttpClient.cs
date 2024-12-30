namespace Satori.Client.Internal;

public class SatoriHttpClient : HttpClient
{
    public SatoriHttpClient(Uri baseUri, string token)
    {
        BaseAddress = baseUri;
        DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }
}