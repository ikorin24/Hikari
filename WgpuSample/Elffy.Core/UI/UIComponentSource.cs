#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Elffy.UI;

[InterpolatedStringHandler]
public ref struct UIComponentSource
{
    private DefaultInterpolatedStringHandler _handler;
    private List<Delegate>? _delegates;
    private int _delegateIndex;

    public UIComponentSource(int literalLength, int formattedCount)
    {
        _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
        _delegates = null;
    }

    public void AppendLiteral(string s) => _handler.AppendLiteral(s);

    public void AppendFormatted(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        _handler.AppendFormatted(type.FullName);
    }

    public void AppendFormatted(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _delegates ??= new List<Delegate>();
        _delegates.Add(action);
        _handler.AppendFormatted(_delegateIndex++);
    }

    public void AppendFormatted<T>(Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _delegates ??= new List<Delegate>();
        _delegates.Add(action);
        _handler.AppendFormatted(_delegateIndex++);
    }

    public void AppendFormatted(string? value) => _handler.AppendFormatted(value);
    public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _handler.AppendFormatted(value, alignment, format);
    public void AppendFormatted(scoped ReadOnlySpan<char> value) => _handler.AppendFormatted(value);
    public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _handler.AppendFormatted(value, alignment, format);
    public void AppendFormatted(object? value) => _handler.AppendFormatted(value);
    public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _handler.AppendFormatted(value, alignment, format);
    public void AppendFormatted<T>(T value) => _handler.AppendFormatted(value);
    public void AppendFormatted<T>(T value, int alignment = 0) => _handler.AppendFormatted(value, alignment);
    public void AppendFormatted<T>(T value, string? format) => _handler.AppendFormatted(value, format);

    internal UIElement BuildAndClear()
    {
        var str = ToStringAndClear();
        var data = GetRuntimeData();
        return Serializer.Deserialize(str, data);
    }

    internal string ToStringAndClear()
    {
        return _handler.ToStringAndClear();
    }

    internal DeserializeRuntimeData GetRuntimeData()
    {
        return new DeserializeRuntimeData(_delegates);
    }

    public static implicit operator UIComponentSource([StringSyntax(StringSyntaxAttribute.Json)] string s)
    {
        var h = new UIComponentSource(s?.Length ?? 0, 0);
        h.AppendLiteral(s ?? "");
        return h;
    }
}

internal static class UIComponentBuilder
{
    public static UIElement Build(this IUIComponent component)
    {
        var source = component.Render();
        var element = source.BuildAndClear();
        if(element.Parent != null) {
            throw new ArgumentException("the element is already in UI tree");
        }
        return element;
    }

    public static void Apply(this IUIComponent component, UIElement targetElement)
    {
        var source = component.Render();
        var data = source.GetRuntimeData();
        using var json = JsonDocument.Parse(source.ToStringAndClear());
        targetElement.ApplyDiff(json.RootElement, data);
    }
}

[global::System.Diagnostics.Conditional("COMPILE_TIME_ONLY")]
[global::System.AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class UIComponentAttribute : global::System.Attribute
{
    public UIComponentAttribute()
    {
    }

    public UIComponentAttribute(string shortName)
    {
    }
}

public interface IUIComponent
{
    bool NeedsToRerender { get; }
    UIComponentSource Render();
}

public readonly ref struct DeserializeRuntimeData
{
    private readonly List<Delegate>? _delegates;

    public static DeserializeRuntimeData None => default;

    internal DeserializeRuntimeData(List<Delegate>? delegates)
    {
        _delegates = delegates;
    }

    public EventSubscription<T> AddEventHandler<T>(Event<T> targetEvent, JsonElement handler)
    {
        var key = handler.GetInt32();
        var d = GetDelegate(key);
        return d switch
        {
            Action<T> action => targetEvent.Subscribe(action),
            Action action => targetEvent.Subscribe(_ => action()),
            null => throw new FormatException("no event handler"),
            _ => throw new FormatException($"event handler type should be {typeof(Action<T>).FullName} or {typeof(Action).FullName}"),
        };
    }

    private Delegate? GetDelegate(int key)
    {
        var delegates = _delegates;
        if(delegates == null || (uint)key >= (uint)delegates.Count) {
            return null;
        }
        return delegates[key];
    }
}

internal interface IReactive
{
    void ApplyDiff(JsonElement element, in DeserializeRuntimeData data);
}
