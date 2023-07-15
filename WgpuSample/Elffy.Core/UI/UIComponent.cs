#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Elffy.UI;

[InterpolatedStringHandler]
public ref struct UIComponent
{
    private DefaultInterpolatedStringHandler _handler;
    private List<Delegate>? _delegates;
    private int _delegateIndex;

    public UIComponent(int literalLength, int formattedCount)
    {
        _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
        _delegates = null;
    }

    public void AppendLiteral(string s) => _handler.AppendLiteral(s);

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

    internal string ToStringAndClear()
    {
        return _handler.ToStringAndClear();
    }

    internal DeserializeRuntimeData GetRuntimeData()
    {
        return new DeserializeRuntimeData(_delegates);
    }

    public static implicit operator UIComponent([StringSyntax(StringSyntaxAttribute.Json)] string s)
    {
        var h = new UIComponent(s?.Length ?? 0, 0);
        h.AppendLiteral(s ?? "");
        return h;
    }
}

[global::System.Diagnostics.Conditional("COMPILE_TIME_ONLY")]
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
    UIComponent Render();
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
