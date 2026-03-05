using System;

namespace DellarteDellaGuerra.Infrastructure.Events;

public interface IEventSubscriber<TEvent>
{
    void Subscribe(Action<TEvent> handler);
}
