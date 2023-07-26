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

    private IReactive? _root;

    public void RenderRoot([StringSyntax(StringSyntaxAttribute.Json)] ReactBuilder builder)
    {
        _uiLayer ??= new UILayer(_screen, 100); // TODO: sort order

        var source = builder.FixAndClear();
        _root = ReactHelper.ApplyDiffOrNew(_root, source, out var isNew);
        if(isNew) {
            switch(_root) {
                case UIElement uiElement: {
                    SetRoot(uiElement);
                    break;
                }
                case IReactComponent component: {
                    var uiElement = component.Build();
                    SetRoot(uiElement);
                    break;
                }
                default: {
                    throw new ArgumentException($"invalid object type: {_root.GetType()}");
                }
            }
        }
        else {
            throw new NotImplementedException();
        }
    }
}
