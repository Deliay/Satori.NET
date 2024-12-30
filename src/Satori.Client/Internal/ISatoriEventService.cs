using Satori.Protocol.Events;

namespace Satori.Client.Internal;

internal interface ISatoriEventService
{
    event EventHandler<Event> EventReceived;

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}