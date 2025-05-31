#if HIKARI_JSON_SERDE
#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Hikari.UI;

partial class UITree
{
    private IReactive? _root;

    public void RenderRoot([StringSyntax(StringSyntaxAttribute.Json)] ObjectSourceBuilder builder)
    {
        var source = builder.ToSourceClear();
        var root = source.Apply(_root, out var applied);
        switch(applied.Result) {
            case ApplySourceResult.InstanceReplaced: {
                switch(root) {
                    case UIElement element: {
                        SetRoot(element);
                        break;
                    }
                    case IReactComponent component: {
                        var element = component.BuildUIElement();
                        SetRoot(element);
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
#endif
