using System;

namespace DellarteDellaGuerra.Infrastructure.Events;

public class EventBus<TEvent> : IEventPublisher<TEvent>, IEventSubscriber<TEvent>
{
    private Action<TEvent>? _handlers;

    public void Subscribe(Action<TEvent> handler) => _handlers += handler;
    public void Publish(TEvent evt) => _handlers?.Invoke(evt);
}
