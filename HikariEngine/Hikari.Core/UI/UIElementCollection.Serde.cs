#if HIKARI_JSON_SERDE
#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using Hikari.Collections;

namespace Hikari.UI;

partial class UIElementCollection : IFromJson<UIElementCollection>, IToJson, IReactive
{
    private readonly List<IReactive> _reactives = new();
    private readonly Dictionary<string, IReactive> _dic = new();

    static partial void RegistorSerdeConstructor()
    {
        Serializer.RegisterConstructor(FromJson);
    }

    private UIElementCollection(List<UIElement> inner, List<IReactive> reactives, Dictionary<string, IReactive> dic)
    {
        _children = inner;
        _reactives = reactives;
        _dic = dic;
    }

    public static UIElementCollection FromJson(in ObjectSource source)
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
                    uiElement.ModelDead.Subscribe(element =>
                    {
                        if(list.Remove(element)) {
                            element.ClearParent();
                        }
                    }).AddTo(uiElement.ModelSubscriptions);
                    break;
                }
                case IReactComponent component: {
                    reactives.Add(component);
                    if(item.HasObjectKey(out var key)) {
                        dic.Add(key, component);
                    }
                    var uiElement = component.BuildUIElement();
                    list.Add(uiElement);
                    uiElement.ModelDead.Subscribe(element =>
                    {
                        if(list.Remove(element)) {
                            element.ClearParent();
                        }
                    }).AddTo(uiElement.ModelSubscriptions);
                    break;
                }
                default: {
                    throw new ArgumentException($"invalid element type: {child.GetType().FullName}");
                }
            }
        }
        return new UIElementCollection(list, reactives, dic);
    }

    public JsonValueKind ToJson(Utf8JsonWriter writer)
    {
        var children = _children;
        writer.WriteStartArray();
        foreach(var child in children) {
            child.ToJson(writer);
        }
        writer.WriteEndArray();
        return JsonValueKind.Array;
    }

    void IReactive.ApplyDiff(in ObjectSource source)
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
}
#endif
