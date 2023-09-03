#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Hikari.UI;

public sealed class UITree
{
    private readonly UILayer _uiLayer;
    private IReactive? _root;

    public Screen Screen => _uiLayer.Screen;

    internal UITree(UILayer uiLayer)
    {
        _uiLayer = uiLayer;
    }

    public void SetRoot(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        _uiLayer.SetRoot(element);
    }

    public void RenderRoot([StringSyntax(StringSyntaxAttribute.Json)] ObjectSourceBuilder builder)
    {
        var source = builder.ToSourceClear();
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

    public bool Terminate()
    {
        return _uiLayer.Terminate();
    }
}
