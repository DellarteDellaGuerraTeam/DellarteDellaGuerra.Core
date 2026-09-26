using System;
using Bannerlord.UIExtenderEx;

namespace DellarteDellaGuerra.Integration.Initialisation;

/// <summary>
/// Owns the UIExtender instance for the Integration assembly. Registration is assembly-wide,
/// so every prefab extension and mixin is enabled here; optional features disable their own
/// extensions afterwards.
/// </summary>
public class DadgUIExtenderRegistrar
{
    private const string UiExtenderModuleName = "DellarteDellaGuerra.Core";

    private readonly UIExtender _uiExtender = UIExtender.Create(UiExtenderModuleName);

    public void RegisterAndEnable()
    {
        _uiExtender.Register(typeof(DadgUIExtenderRegistrar).Assembly);
        _uiExtender.Enable();
    }

    public void Disable(Type extensionType)
    {
        _uiExtender.Disable(extensionType);
    }
}
