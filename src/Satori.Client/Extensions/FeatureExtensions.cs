using Satori.Protocol.Models;

namespace Satori.Client.Extensions;

public static class FeatureExtensions
{
    private static SatoriBot CreateBot(this SatoriClient client, Login login)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(login.Platform);
        ArgumentException.ThrowIfNullOrWhiteSpace(login.SelfId);
        
        return new SatoriBot(client, login.Platform, login.SelfId);
    }
    
    public static async Task<SatoriBot> GetBotAsync(this SatoriClient client, CancellationToken cancellationToken = default)
    {
        var login = await client.GetLoginAsync(cancellationToken);
        
        return CreateBot(client, login);
    }

}