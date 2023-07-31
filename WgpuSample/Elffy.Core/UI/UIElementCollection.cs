#nullable enable
using Elffy.Effective;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    private readonly List<IReactive> _reactives;
    private readonly Dictionary<string, IReactive> _dic;

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

    public int Count => _children.Count;

    static UIElementCollection() => Serializer.RegisterConstructor(FromJson);

    public UIElementCollection()
    {
        _children = new List<UIElement>();
        _reactives = new List<IReactive>();
        _dic = new Dictionary<string, IReactive>();
    }

    private UIElementCollection(List<UIElement> inner, List<IReactive> reactives, Dictionary<string, IReactive> dic)
    {
        _children = inner;
        _reactives = reactives;
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

    public Enumerator GetEnumerator() => new Enumerator(_children.GetEnumerator());

    IEnumerator<UIElement> IEnumerable<UIElement>.GetEnumerator() => _children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _children.GetEnumerator();

    public static UIElementCollection FromJson(in ReactSource source)
    {
        var list = new List<UIElement>(source.GetArrayLength());
        var reactives = new List<IReactive>();
        var dic = new Dictionary<string, IReactive>();
        foreach(var item in source.EnumerateArray()) {
            var child = Serializer.Instantiate(item, null);
            switch(child) {
                case UIElement uiElement: {
                    reactives.Add(uiElement);
                    if(item.HasObjectKey(out var key)) {
                        dic.Add(key, uiElement);
                    }
                    list.Add(uiElement);
                    break;
                }
                case IReactComponent component: {
                    reactives.Add(component);
                    if(item.HasObjectKey(out var key)) {
                        dic.Add(key, component);
                    }
                    var uiElement = component.BuildUIElement();
                    list.Add(uiElement);
                    break;
                }
                default: {
                    throw new ArgumentException($"invalid element type: {child.GetType().FullName}");
                }
            }
        }
        return new UIElementCollection(list, reactives, dic);
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

    void IReactive.ApplyDiff(in ReactSource source)
    {
        var reactives = _reactives;
        var i = 0;
        var childCount = source.GetArrayLength();
        using(var tmpMemory = new RefTypeRentMemory<IReactive>(childCount, out var tmp)) {
            foreach(var item in source.EnumerateArray()) {
                IReactive? current;
                if(item.HasObjectKey(out var key)) {
                    if(_dic.TryGetValue(key, out current) == false) {
                        current = null;
                    }
                }
                else {
                    current = null;
                }
                tmp[i] = item.Apply(current, out _);
                i++;
            }
            reactives.Clear();
            reactives.AddRange(tmp);
        }
    }

    public struct Enumerator : IEnumerator<UIElement>
    {
        private List<UIElement>.Enumerator _enumerator;

        public UIElement Current => _enumerator.Current;

        object IEnumerator.Current => ((IEnumerator)_enumerator).Current;

        internal Enumerator(List<UIElement>.Enumerator enumerator)
        {
            _enumerator = enumerator;
        }

        public void Dispose() => _enumerator.Dispose();

        public bool MoveNext() => _enumerator.MoveNext();

        void IEnumerator.Reset() => ((IEnumerator)_enumerator).Reset();
    }
}
