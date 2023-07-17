#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.UI;

public sealed class UITree
{
    private readonly Screen _screen;

    private UILayer? _uiLayer;

    public Screen Screen => _screen;

    internal UITree(Screen screen)
    {
        _screen = screen;
    }

    public void SetRoot(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        _uiLayer ??= new UILayer(_screen, 100); // TODO: sort order
        _uiLayer.SetRoot(element);
    }

    public void RenderRoot([StringSyntax(StringSyntaxAttribute.Json)] UIComponentSource source)
    {
        _uiLayer ??= new UILayer(_screen, 100); // TODO: sort order
        var component = new ImmutableUIComponent(source);
        _uiLayer.RenderRoot(component);
    }
}
