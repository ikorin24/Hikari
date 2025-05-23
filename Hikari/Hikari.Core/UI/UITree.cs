﻿#nullable enable
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Hikari.UI;

public sealed class UITree
{
    private readonly Screen _screen;
    private UIElement? _rootElement;
    private bool _isLayoutDirty = false;
    private IReactive? _root;

    private static readonly ConcurrentDictionary<Type, Func<Screen, Own<IUIMaterial>>> _materialProviders = new();

    public Screen Screen => _screen;

    internal UITree(Screen screen)
    {
        _screen = screen;
        screen.Resized.Subscribe(arg =>
        {
            _isLayoutDirty = true;
        });
        screen.EarlyUpdate.Subscribe(screen =>
        {
            var rootElement = _rootElement;
            if(rootElement == null) {
                return;
            }

            var isLayoutDirty = _isLayoutDirty;
            _isLayoutDirty = false;
            var mouse = screen.Mouse;
            var dummyInfo = UIElementInfo.Default;
            var screenSize = screen.ClientSize;
            var scaleFactor = screen.ScaleFactor;
            var contentArea = new RectF
            {
                X = 0,
                Y = 0,
                Width = screenSize.X,
                Height = screenSize.Y,
            };
            var childrenFlowInfo = dummyInfo.Flow.NewChildrenFlowInfo(contentArea);
            rootElement.UpdateLayout(isLayoutDirty, dummyInfo, contentArea, ref childrenFlowInfo, mouse, scaleFactor);
        });

        screen.PrepareForRender.Subscribe(screen =>
        {
            var screenSize = screen.ClientSize;
            var scaleFactor = screen.ScaleFactor;
            var uiProjection = Matrix4.ReversedZ.OrthographicProjection(0, (float)screenSize.X, 0, (float)screenSize.Y, 0, 1f);

            var rootElement = _rootElement;
            if(rootElement == null) {
                return;
            }

            uint i = 0;
            Recurse(rootElement, screenSize, scaleFactor, in uiProjection, ref i);

            static void Recurse(UIElement element, Vector2u screenSize, float scaleFactor, in Matrix4 uiProjection, ref uint index)
            {
                //var depth = float.Min(1, index++ / 1000f);  // TODO:
                element.UpdateMaterial(screenSize, scaleFactor, uiProjection, 0f);
                foreach(var child in element.Children) {
                    Recurse(child, screenSize, scaleFactor, in uiProjection, ref index);
                }
            }

        });
    }

    internal static bool RegisterMaterial<T>(Func<Screen, Own<IUIMaterial>> provider) where T : UIElement
    {
        ArgumentNullException.ThrowIfNull(provider);
        return _materialProviders.TryAdd(typeof(T), provider);
    }

    internal Own<IUIMaterial> GetRegisteredMaterial(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var provider = _materialProviders[type];
        return provider.Invoke(_screen);
    }

    public void SetRoot(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if(element.Parent != null) {
            throw new ArgumentException("the element is already in UI tree");
        }
        element.CreateModel(this);
        element.ModelAlive.Subscribe(element =>
        {
            _rootElement?.Model?.Terminate();
            _rootElement = element;
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
