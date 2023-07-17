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
    private readonly bool _isReadOnly;

    public UIComponentSource(int literalLength, int formattedCount)
    {
        _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
        _delegates = null;
        _delegateIndex = 0;
        _isReadOnly = false;
    }

    internal UIComponentSource(string source, DeserializeRuntimeData data)
    {
        _handler = $"{source}";
        (_delegates, _delegateIndex) = data;
        _isReadOnly = true;
    }

    private void ThrowIfReadOnly()
    {
        if(_isReadOnly) {
            Throw();
            [DoesNotReturn] static void Throw() => throw new InvalidOperationException("the source is read only");
        }
    }

    public void AppendLiteral(string s)
    {
        ThrowIfReadOnly();
        _handler.AppendLiteral(s);
    }

    public void AppendFormatted(Type type)
    {
        ThrowIfReadOnly();
        ArgumentNullException.ThrowIfNull(type);
        RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        _handler.AppendLiteral("\"");
        _handler.AppendFormatted(type.FullName);
        _handler.AppendLiteral("\"");
    }

    public void AppendFormatted(Action action)
    {
        ThrowIfReadOnly();
        ArgumentNullException.ThrowIfNull(action);
        _delegates ??= new List<Delegate>();
        _delegates.Add(action);
        _handler.AppendFormatted(_delegateIndex++);
    }

    public void AppendFormatted<T>(Action<T> action)
    {
        ThrowIfReadOnly();
        ArgumentNullException.ThrowIfNull(action);
        _delegates ??= new List<Delegate>();
        _delegates.Add(action);
        _handler.AppendFormatted(_delegateIndex++);
    }

    public void AppendFormatted(sbyte value)
    {
        ThrowIfReadOnly();
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(byte value)
    {
        ThrowIfReadOnly();
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(short value)
    {
        ThrowIfReadOnly();
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(ushort value)
    {
        ThrowIfReadOnly();
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(int value)
    {
        ThrowIfReadOnly();
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(uint value)
    {
        ThrowIfReadOnly();
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(long value)
    {
        ThrowIfReadOnly();
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(ulong value)
    {
        ThrowIfReadOnly();
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(float value)
    {
        ThrowIfReadOnly();
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(double value)
    {
        ThrowIfReadOnly();
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(decimal value)
    {
        ThrowIfReadOnly();
        _handler.AppendFormatted(value);
    }

    public void AppendFormatted<T>(T value) where T : IToJson
    {
        ThrowIfReadOnly();
        var json = value.ToJson();
        var jsonStr = json?.ToJsonString();
        _handler.AppendFormatted(jsonStr);
    }

    public void AppendFormatted(string? value)
    {
        ThrowIfReadOnly();
        _handler.AppendLiteral("\"");
        _handler.AppendFormatted(value);
        _handler.AppendLiteral("\"");
    }

    public void AppendFormatted(scoped ReadOnlySpan<char> value)
    {
        ThrowIfReadOnly();
        _handler.AppendLiteral("\"");
        _handler.AppendFormatted(value);
        _handler.AppendLiteral("\"");
    }

    public FixedComponentSource FixAndClear()
    {
        return new FixedComponentSource(_handler.ToStringAndClear(), new DeserializeRuntimeData(_delegates));
    }

    public static implicit operator UIComponentSource([StringSyntax(StringSyntaxAttribute.Json)] string s)
    {
        var h = new UIComponentSource(s?.Length ?? 0, 0);
        h.AppendLiteral(s ?? "");
        return h;
    }
}

public readonly struct FixedComponentSource : IEquatable<FixedComponentSource>
{
    private readonly string? _str;
    private readonly DeserializeRuntimeData _data;

    public string Str => _str ?? "";
    public DeserializeRuntimeData RuntimeData => _data;

    internal FixedComponentSource(string str, DeserializeRuntimeData data)
    {
        _str = str;
        _data = data;
    }

    public object Deserialize()
    {
        return Serializer.Deserialize(_str ?? "", _data);
    }

    public override bool Equals(object? obj) => obj is FixedComponentSource source && Equals(source);

    public bool Equals(FixedComponentSource other) => _str == other._str && _data.Equals(other._data);

    public override int GetHashCode() => HashCode.Combine(_str, _data);
}

internal sealed class FixedUIComponent : IUIComponent
{
    private readonly FixedComponentSource _fixedSource;

    public bool NeedsToRerender => false;

    internal FixedUIComponent(FixedComponentSource fixedSource)
    {
        _fixedSource = fixedSource;
    }

    public FixedComponentSource Fix() => _fixedSource;
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
    FixedComponentSource Fix();
}

public readonly struct DeserializeRuntimeData : IEquatable<DeserializeRuntimeData>
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

    public override bool Equals(object? obj) => obj is DeserializeRuntimeData data && Equals(data);

    public bool Equals(DeserializeRuntimeData other) => _delegates == other._delegates;

    public override int GetHashCode() => HashCode.Combine(_delegates);
}

internal interface IReactive
{
    void ApplyDiff(JsonElement element, in DeserializeRuntimeData data);
}
