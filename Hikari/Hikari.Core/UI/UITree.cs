#nullable enable
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Hikari.UI;

public sealed class UITree
{
    private readonly Screen _screen;
    //    private readonly UIDescriptor _desc;
    private UIElement? _rootElement;
    //    private bool _isLayoutDirty = false;

    private IReactive? _root;

    private static readonly ConcurrentDictionary<Type, Func<Screen, UIShader>> _shaderProviders = new();

    public Screen Screen => _screen;

    internal UITree(Screen screen)
    {
        _screen = screen;
    }

    internal static bool RegisterShader<T>(Func<Screen, UIShader> provider) where T : UIElement
    {
        ArgumentNullException.ThrowIfNull(provider);
        return _shaderProviders.TryAdd(typeof(T), provider);
    }

    internal UIShader GetRegisteredShader(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var provider = _shaderProviders[type];
        return provider.Invoke(_screen);
    }

    public void SetRoot(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if(element.Parent != null) {
            throw new ArgumentException("the element is already in UI tree");
        }
        element.CreateModel(this);
        element.ModelAlive.Subscribe(model =>
        {
            _rootElement?.Model?.Terminate();
            _rootElement = model.Element;
        });
    }

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
