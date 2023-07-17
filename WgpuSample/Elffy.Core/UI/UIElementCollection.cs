#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;

namespace Elffy.UI;

public sealed class UIElementCollection
    : IEnumerable<UIElement>,
      IFromJson<UIElementCollection>,
      IToJson
{
    private UIElement? _parent;
    private readonly List<UIElement> _children;

    internal UIElement? Parent
    {
        get => _parent;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if(Interlocked.CompareExchange(ref _parent, value, null) != null) {
                ThrowInvalidInstance();
            }
            var layer = value.Model?.Layer;
            foreach(var child in _children) {
                child.SetParent(value);
                if(layer != null) {
                    child.CreateModel(layer);
                }
            }
        }
    }

    public UIElement this[int index]
    {
        get => _children[index];
    }

    static UIElementCollection() => Serializer.RegisterConstructor(FromJson);

    public UIElementCollection()
    {
        _children = new List<UIElement>();
    }

    private UIElementCollection(List<UIElement> inner)
    {
        _children = inner;
    }

    internal UIElementCollection(ReadOnlySpan<UIElement> elements)
    {
        _children = new List<UIElement>(elements.Length);
        foreach(var element in elements) {
            ArgumentNullException.ThrowIfNull(element);
            _children.Add(element);
        }
    }

    public void Add(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        _children.Add(element);
        var parent = _parent;
        if(parent != null) {
            element.SetParent(parent);
            var layer = parent.Model?.Layer;
            if(layer != null) {
                element.CreateModel(layer);
            }
        }
    }

    [DoesNotReturn]
    private static void ThrowInvalidInstance() => throw new InvalidOperationException("invalid instance");

    public IEnumerator<UIElement> GetEnumerator()
    {
        // TODO: 
        return _children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _children.GetEnumerator();
    }

    public static UIElementCollection FromJson(JsonElement element, in DeserializeRuntimeData data)
    {
        var list = new List<UIElement>(element.GetArrayLength());
        foreach(var item in element.EnumerateArray()) {
            var child = Serializer.Instantiate(item);
            switch(child) {
                case UIElement uiElement: {
                    list.Add(uiElement);
                    break;
                }
                case IReactComponent component: {
                    var uiElement = component.Build();
                    list.Add(uiElement);
                    break;
                }
                default: {
                    throw new ArgumentException($"invalid element type: {child.GetType().FullName}");
                }
            }
        }
        return new UIElementCollection(list);
    }

    public JsonNode? ToJson()
    {
        var children = _children;
        var array = new JsonArray();
        foreach(var child in children) {
            array.Add(child.ToJson());
        }
        return array;
    }
}
