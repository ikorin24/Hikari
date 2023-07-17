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

    internal UIComponentSource(string source, DeserializeRuntimeData data)
    {
        _handler = $"{source}";
        (_delegates, _delegateIndex) = data;
    }

    public void AppendLiteral(string s) => _handler.AppendLiteral(s);

    public void AppendFormatted(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        _handler.AppendLiteral("\"");
        _handler.AppendFormatted(type.FullName);
        _handler.AppendLiteral("\"");
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

    public void AppendFormatted(sbyte value) => _handler.AppendFormatted(value);
    public void AppendFormatted(byte value) => _handler.AppendFormatted(value);
    public void AppendFormatted(short value) => _handler.AppendFormatted(value);
    public void AppendFormatted(ushort value) => _handler.AppendFormatted(value);
    public void AppendFormatted(int value) => _handler.AppendFormatted(value);
    public void AppendFormatted(uint value) => _handler.AppendFormatted(value);
    public void AppendFormatted(long value) => _handler.AppendFormatted(value);
    public void AppendFormatted(ulong value) => _handler.AppendFormatted(value);
    public void AppendFormatted(float value) => _handler.AppendFormatted(value);
    public void AppendFormatted(double value) => _handler.AppendFormatted(value);
    public void AppendFormatted(decimal value) => _handler.AppendFormatted(value);

    public void AppendFormatted<T>(T value) where T : IToJson
    {
        var json = value.ToJson();
        var jsonStr = json?.ToJsonString();
        _handler.AppendFormatted(jsonStr);
    }

    public void AppendFormatted(string? value)
    {
        _handler.AppendLiteral("\"");
        _handler.AppendFormatted(value);
        _handler.AppendLiteral("\"");
    }

    public void AppendFormatted(scoped ReadOnlySpan<char> value)
    {
        _handler.AppendLiteral("\"");
        _handler.AppendFormatted(value);
        _handler.AppendLiteral("\"");
    }

    internal string ToStringAndClear(out DeserializeRuntimeData data)
    {
        data = new DeserializeRuntimeData(_delegates);
        return _handler.ToStringAndClear();
    }

    internal object DeserializeAndClear()
    {
        var data = new DeserializeRuntimeData(_delegates);
        return Serializer.Deserialize(_handler.ToStringAndClear(), data);
    }

    public static implicit operator UIComponentSource([StringSyntax(StringSyntaxAttribute.Json)] string s)
    {
        var h = new UIComponentSource(s?.Length ?? 0, 0);
        h.AppendLiteral(s ?? "");
        return h;
    }
}

internal sealed class ImmutableUIComponent : IUIComponent
{
    private readonly string _source;
    private readonly DeserializeRuntimeData _data;

    public bool NeedsToRerender => false;

    public ImmutableUIComponent(UIComponentSource source)
    {
        _source = source.ToStringAndClear(out _data);
    }

    public UIComponentSource Render() => new UIComponentSource(_source, _data);
}

[global::System.Diagnostics.Conditional("COMPILE_TIME_ONLY")]
[global::System.AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class UIComponentAttribute : global::System.Attribute
{
    public UIComponentAttribute()
    {
    }
}

public interface IUIComponent
{
    bool NeedsToRerender { get; }
    UIComponentSource Render();
}

public readonly struct DeserializeRuntimeData
{
    private readonly List<Delegate>? _delegates;

    public static DeserializeRuntimeData None => default;

    internal DeserializeRuntimeData(List<Delegate>? delegates)
    {
        _delegates = delegates;
    }

    internal void Deconstruct(out List<Delegate>? delegates, out int delegateIndex)
    {
        delegates = _delegates;
        delegateIndex = delegates?.Count ?? 0;
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
