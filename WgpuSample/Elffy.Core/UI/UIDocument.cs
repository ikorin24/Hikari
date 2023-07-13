#nullable enable
using Elffy;
using System;

namespace Elffy.UI;

public sealed class UIDocument
{
    private readonly Screen _screen;

    private UILayer? _uiLayer;

    public Screen Screen => _screen;

    internal UIDocument(Screen screen)
    {
        _screen = screen;
    }

    public void AddRoot(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        _uiLayer ??= new UILayer(_screen, 100); // TODO: sort order
        _uiLayer.AddRootElement(element);
    }
}
