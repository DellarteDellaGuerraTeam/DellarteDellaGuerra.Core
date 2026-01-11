using System;

namespace DellarteDellaGuerra.Infrastructure.Events;

public class OnSubModuleLoadEventPubSub : IOnSubModuleLoadEventSubscriber, IOnSubModuleLoadEventPublisher
{
    private Action _onSubModuleLoaded;

    public void Subscribe(Action onSubModuleLoaded)
    {
        _onSubModuleLoaded += onSubModuleLoaded;
    }

    public void Publish()
    {
        _onSubModuleLoaded?.Invoke();
    }
}