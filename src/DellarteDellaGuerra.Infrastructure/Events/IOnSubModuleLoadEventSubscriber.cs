using System;

namespace DellarteDellaGuerra.Infrastructure.Events;

public interface IOnSubModuleLoadEventSubscriber
{
    void Subscribe(Action onSubModuleLoaded);
}