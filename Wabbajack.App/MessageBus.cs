using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Wabbajack.App.Messages;

namespace Wabbajack.App;

public interface IMessageBus
{
    public void Send<T>(T message);
}

public class MessageBus : IMessageBus
{
    private readonly ILogger<MessageBus> _logger;
    private readonly IReceiverMarker[] _receivers;

    public MessageBus(ILogger<MessageBus> logger, IEnumerable<IReceiverMarker> receivers)
    {
        Instance = this;
        _receivers = receivers.ToArray();
        _logger = logger;
    }

    public static IMessageBus Instance { get; set; }

    public void Send<T>(T message)
    {
        AvaloniaScheduler.Instance.Schedule(message, TimeSpan.FromMilliseconds(200), (_, msg) =>
        {
            foreach (var receiver in _receivers.OfType<IReceiver<T>>())
            {
                _logger.LogInformation("Sending {msg} to {receiver}", msg, receiver);
                try
                {
                    receiver.Receive(msg);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Failed sending {msg} to {receiver}", msg, receiver);
                }
            }

            return Disposable.Empty;
        });
    }
}