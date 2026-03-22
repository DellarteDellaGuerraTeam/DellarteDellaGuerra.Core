namespace DellarteDellaGuerra.Infrastructure.Events;

public interface IEventPublisher<TEvent>
{
    void Publish(TEvent evt);
}
