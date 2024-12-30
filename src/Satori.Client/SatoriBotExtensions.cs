using Satori.Protocol.Elements;
using Satori.Protocol.Models;

namespace Satori.Client;

public static class SatoriBotExtensions
{
    public static Task<Message[]> CreateMessageAsync(this SatoriBot bot, string channelId, Element content, 
        CancellationToken cancellationToken = default)
    {
        return bot.CreateMessageAsync(channelId, [content], cancellationToken);
    }
}