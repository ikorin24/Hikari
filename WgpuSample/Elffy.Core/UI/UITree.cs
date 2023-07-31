#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.UI;

public sealed class UITree
{
    private readonly Screen _screen;
    private UILayer? _uiLayer;
    private IReactive? _root;

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

    public void RenderRoot([StringSyntax(StringSyntaxAttribute.Json)] ReactBuilder builder)
    {
        _uiLayer ??= new UILayer(_screen, 100); // TODO: sort order

        var source = builder.FixAndClear();
        var result = source.ApplyTo(ref _root, out var old);
        switch(result) {
            case ApplySourceResult.InstanceReplaced: {
                var uiElement = _root switch
                {
                    UIElement e => e,
                    IReactComponent c => c.BuildUIElement(),
                    _ => throw new ArgumentException($"invalid object type: {_root.GetType()}")
                };
                (old as IReactComponent)?.OnUnmount();
                _uiLayer.SetRoot(uiElement);
                (_root as IReactComponent)?.OnMount();
                break;
            }
            case ApplySourceResult.PropertyDiffApplied:
            case ApplySourceResult.ArrayDiffApplied:
            default: {
                break;
            }
        }
    }
}
