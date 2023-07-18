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
      IToJson,
      IReactive
{
    private UIElement? _parent;
    private readonly List<UIElement> _children;
    private readonly Dictionary<string, DicValue> _dic;

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
        _dic = new Dictionary<string, DicValue>();
    }

    private UIElementCollection(List<UIElement> inner, Dictionary<string, DicValue> dic)
    {
        _children = inner;
        _dic = dic;
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
        var dic = new Dictionary<string, DicValue>();
        foreach(var item in element.EnumerateArray()) {
            var child = Serializer.Instantiate(item, data);
            //dic.Add(item.GetProperty("@key").GetStringNotNull(), child);
            switch(child) {
                case UIElement uiElement: {
                    list.Add(uiElement);
                    dic.Add(item.GetProperty("@key").GetStringNotNull(), new DicValue(uiElement));
                    break;
                }
                case IReactComponent component: {
                    var uiElement = component.Build();
                    list.Add(uiElement);
                    dic.Add(item.GetProperty("@key").GetStringNotNull(), new DicValue(component));
                    break;
                }
                default: {
                    throw new ArgumentException($"invalid element type: {child.GetType().FullName}");
                }
            }
        }
        return new UIElementCollection(list, dic);
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

    void IReactive.ApplyDiff(JsonElement element, in DeserializeRuntimeData data)
    {
        throw new NotImplementedException();
    }

    private readonly struct DicValue : IEquatable<DicValue>
    {
        private readonly bool _isUIElement;
        private readonly object _object;

        public DicValue(UIElement uIElement)
        {
            _isUIElement = true;
            _object = uIElement;
        }

        public DicValue(IReactComponent component)
        {
            _isUIElement = false;
            _object = component;
        }

        public override bool Equals(object? obj) => obj is DicValue value && Equals(value);

        public bool Equals(DicValue other)
        {
            return _isUIElement == other._isUIElement && ReferenceEquals(_object, other._object);
        }

        public override int GetHashCode() => HashCode.Combine(_isUIElement, _object);
    }
}
