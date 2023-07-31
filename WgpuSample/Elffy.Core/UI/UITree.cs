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
        var root = source.Apply(_root, out var applied);
        switch(applied.Result) {
            case ApplySourceResult.InstanceReplaced: {
                switch(root) {
                    case UIElement element: {
                        _uiLayer.SetRoot(element);
                        break;
                    }
                    case IReactComponent component: {
                        var element = component.BuildUIElement();
                        _uiLayer.SetRoot(element);
                        break;
                    }
                    default: {
                        throw new ArgumentException($"invalid object type: {root.GetType()}");
                    }
                }
                _root = root;
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
