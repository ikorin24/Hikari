#nullable enable
using Elffy.Effective.Unsafes;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Elffy.UI;

[InterpolatedStringHandler]
public ref struct ReactBuilder
{
    private DefaultInterpolatedStringHandler _handler;
    private List<Delegate?>? _delegates;
    private List<Type>? _types;
    private readonly bool _isReadOnly;

    public ReactBuilder(int literalLength, int formattedCount)
    {
        _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
        _delegates = null;
        _types = null;
        _isReadOnly = false;
    }

    //internal ReactBuilder(string source, DeserializeRuntimeData data)
    //{
    //    _handler = $"{source}";
    //    (_delegates, _delegateIndex) = data;
    //    _isReadOnly = true;
    //}

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
        _types ??= new List<Type>();
        var index = _types.Count;
        _types.Add(type);
        _handler.AppendFormatted(index);
    }

    public void AppendFormatted(Action? action)
    {
        ThrowIfReadOnly();
        _delegates ??= new List<Delegate?>();
        var index = _delegates.Count;
        _delegates.Add(action);
        _handler.AppendFormatted(index);
    }

    public void AppendFormatted<T>(Action<T>? action)
    {
        ThrowIfReadOnly();
        _delegates ??= new List<Delegate?>();
        var index = _delegates.Count;
        _delegates.Add(action);
        _handler.AppendFormatted(index);
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

    public void AppendFormatted<T>(T value, JsonMarker _ = default) where T : IToJson
    {
        ThrowIfReadOnly();
        var json = value.ToJson();
        var jsonStr = json?.ToJsonString();
        _handler.AppendFormatted(jsonStr);
    }

    public void AppendFormatted<T>(T value, EnumMarker _ = default) where T : struct, Enum
    {
        ThrowIfReadOnly();
        var json = value.ToJson();
        var jsonStr = json?.ToJsonString();
        _handler.AppendFormatted(jsonStr);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public record struct EnumMarker;
    [EditorBrowsable(EditorBrowsableState.Never)]
    public record struct JsonMarker;

    public ReactSource FixAndClear()
    {
        var data = new DeserializeRuntimeData(_delegates, _types);
        return new ReactSource(_handler.ToStringAndClear(), data);
    }

    public static implicit operator ReactBuilder([StringSyntax(StringSyntaxAttribute.Json)] string s)
    {
        var h = new ReactBuilder(s?.Length ?? 0, 0);
        h.AppendLiteral(s ?? "");
        return h;
    }
}

public readonly struct ReactSource : IEquatable<ReactSource>
{
    private readonly JsonElement _element;
    private readonly DeserializeRuntimeData _data;

    internal DeserializeRuntimeData RuntimeData => _data;

    internal JsonElement Element => _element;

    public JsonValueKind ValueKind => _element.ValueKind;

    public bool IsArray => _element.ValueKind == JsonValueKind.Array;
    public bool IsObject => _element.ValueKind == JsonValueKind.Object;

    public Type? ObjectType
    {
        get
        {
            if(_element.ValueKind == JsonValueKind.Object && _element.TryGetProperty("@type"u8, out var type)) {
                return type.ValueKind switch
                {
                    JsonValueKind.Number => _data.GetType(type),
                    _ => null,
                };
            }
            return null;
        }
    }

    public string? ObjectKey
    {
        get
        {
            return _element.ValueKind switch
            {
                JsonValueKind.Object => _element.TryGetProperty("@key"u8, out var key) switch
                {
                    true => key.ValueKind switch
                    {
                        JsonValueKind.String => key.GetString(),
                        _ => null,
                    },
                    false => null,
                },
                _ => null,
            };
        }
    }

    internal ReactSource(string str, DeserializeRuntimeData data)
    {
        _data = data;

        // Clone the root element to make the lifetime permanent.
        using var json = JsonDocument.Parse(str, Serializer.ParseOptions);
        _element = json.RootElement.Clone();
    }

    private ReactSource(JsonElement element, DeserializeRuntimeData data)
    {
        _element = element;
        _data = data;
    }

    public bool HasObjectKey([MaybeNullWhen(false)] out string key)
    {
        key = ObjectKey;
        return key != null;
    }

    public bool TryGetProperty(string propertyName, out ReactSource value)
    {
        if(_element.TryGetProperty(propertyName, out var prop)) {
            value = new ReactSource(prop, _data);
            return true;
        }
        value = default;
        return false;
    }

    public string GetStringNotNull()
    {
        var str = _element.GetString();
        if(str == null) {
            Throw();
        }
        return str;

        [DoesNotReturn]
        static void Throw() => throw new FormatException("null");
    }

    public T GetNumber<T>() where T : struct, System.Numerics.INumber<T>
    {
        if(typeof(T) == typeof(Decimal)) { return UnsafeEx.As<Decimal, T>(_element.GetDecimal()); }
        if(typeof(T) == typeof(Byte)) { return UnsafeEx.As<Byte, T>(_element.GetByte()); }
        if(typeof(T) == typeof(Char)) { return UnsafeEx.As<Char, T>((char)_element.GetInt32()); }
        if(typeof(T) == typeof(Double)) { return UnsafeEx.As<Double, T>(_element.GetDouble()); }
        if(typeof(T) == typeof(Half)) { return UnsafeEx.As<Half, T>((Half)_element.GetSingle()); }
        if(typeof(T) == typeof(Int16)) { return UnsafeEx.As<Int16, T>(_element.GetInt16()); }
        if(typeof(T) == typeof(Int32)) { return UnsafeEx.As<Int32, T>(_element.GetInt32()); }
        if(typeof(T) == typeof(Int64)) { return UnsafeEx.As<Int64, T>(_element.GetInt64()); }
        if(typeof(T) == typeof(IntPtr)) { return UnsafeEx.As<IntPtr, T>((IntPtr)_element.GetInt64()); }
        if(typeof(T) == typeof(SByte)) { return UnsafeEx.As<SByte, T>(_element.GetSByte()); }
        if(typeof(T) == typeof(Single)) { return UnsafeEx.As<Single, T>(_element.GetSingle()); }
        if(typeof(T) == typeof(UInt16)) { return UnsafeEx.As<UInt16, T>(_element.GetUInt16()); }
        if(typeof(T) == typeof(UInt32)) { return UnsafeEx.As<UInt32, T>(_element.GetUInt32()); }
        if(typeof(T) == typeof(UInt64)) { return UnsafeEx.As<UInt64, T>(_element.GetUInt64()); }
        if(typeof(T) == typeof(UIntPtr)) { return UnsafeEx.As<UIntPtr, T>((UIntPtr)_element.GetUInt64()); }

        // Int128, UInt128, NFloat
        throw new NotSupportedException($"{typeof(T)} is not supported");
    }

    public bool GetBoolean() => _element.GetBoolean();

    public DateTime GetDateTime() => _element.GetDateTime();
    public DateTimeOffset GetDateTimeOffset() => _element.GetDateTimeOffset();
    public Guid GetGuid() => _element.GetGuid();
    public T ToEnum<T>() where T : struct, Enum => Enum.Parse<T>(GetStringNotNull(_element));
    public object ToEnum(Type type) => Enum.Parse(type, GetStringNotNull(_element));

    private static string GetStringNotNull(JsonElement element)
    {
        var str = element.GetString();
        if(str == null) {
            Throw();
        }
        return str;

        [DoesNotReturn]
        static void Throw() => throw new FormatException("null");
    }


    public T ApplyProperty<T>(string propertyName, in T current, in T defaultValue)
    {
        if(TryGetProperty(propertyName, out var propertyValue)) {
            return ReactHelper.ApplyDiffOrNew<T>(current, propertyValue);
        }
        return defaultValue;
    }

    //public T ApplyProperty<T>(string propertyName, in T current, Func<T> defaultFactory)
    //{
    //    ArgumentNullException.ThrowIfNull(defaultFactory);
    //    if(TryGetProperty(propertyName, out var propertyValue)) {
    //        return ReactHelper.ApplyDiffOrNew<T>(current, propertyValue);
    //    }
    //    return defaultFactory.Invoke();
    //}

    public int GetArrayLength() => _element.GetArrayLength();

    public ArrayEnumerable EnumerateArray() => new ArrayEnumerable(this);

    public T Instantiate<T>()
    {
        return Serializer.Instantiate<T>(this);
    }

    public object Instantiate(Type? type)
    {
        return Serializer.Instantiate(this, type);
    }

    [DoesNotReturn]
    public void ThrowInvalidFormat() => throw new FormatException(ToDebugString());

    public string ToDebugString()
    {
        var delegates = _data.Delegates;
        var types = _data.Types;

        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions
        {
            Indented = true,
        };
        using var writer = new Utf8JsonWriter(buffer, options);
        writer.WriteStartObject();
        {
            writer.WritePropertyName("element");
            _element.WriteTo(writer);
            writer.WriteStartObject("data");
            {
                writer.WriteStartArray("delegates");
                {
                    foreach(var d in delegates) {
                        writer.WriteStringValue(d?.Method?.Name);
                    }
                }
                writer.WriteEndArray();
                writer.WriteStartArray("types");
                {
                    foreach(var t in types) {
                        writer.WriteStringValue(t.FullName);
                    }
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
        writer.Flush();
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public override bool Equals(object? obj)
    {
        return obj is ReactSource source && Equals(source);
    }

    public bool Equals(ReactSource other)
    {
        return EqualityComparer<JsonElement>.Default.Equals(_element, other._element) &&
               _data.Equals(other._data);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_element, _data);
    }

    public record struct ArrayEnumerable(ReactSource Source) : IEnumerable<ReactSource>
    {
        public ArrayEnumerator GetEnumerator() => new ArrayEnumerator(Source);
        IEnumerator<ReactSource> IEnumerable<ReactSource>.GetEnumerator() => new ArrayEnumerator(Source);
        IEnumerator IEnumerable.GetEnumerator() => new ArrayEnumerator(Source);
    }

    public struct ArrayEnumerator : IEnumerator<ReactSource>
    {
        private readonly DeserializeRuntimeData _data;
        private JsonElement.ArrayEnumerator _enumerator;

        public ArrayEnumerator(ReactSource source)
        {
            _data = source.RuntimeData;
            _enumerator = source.Element.EnumerateArray();
        }

        public ReactSource Current => new ReactSource(_enumerator.Current, _data);

        object IEnumerator.Current => throw new NotImplementedException();

        public void Dispose() => _enumerator.Dispose();

        public bool MoveNext() => _enumerator.MoveNext();

        public void Reset() => _enumerator.Reset();
    }
}

//internal sealed class FixedReactComponent : IReactComponent
//{
//    private readonly ReactSource _source;

//    public bool NeedsToRerender => false;

//    internal FixedReactComponent(ReactSource source)
//    {
//        _source = source;
//    }

//    public ReactSource GetReactSource() => _source;

//    public void RenderCompleted()
//    {
//        // nop
//    }
//}

[global::System.Diagnostics.Conditional("COMPILE_TIME_ONLY")]
[global::System.AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ReactComponentAttribute : global::System.Attribute
{
    public ReactComponentAttribute()
    {
    }
}

public interface IReactComponent : IReactive
{
    bool NeedsToRerender { get; }
    ReactSource GetReactSource();
    void RenderCompleted();
}

internal readonly struct DeserializeRuntimeData : IEquatable<DeserializeRuntimeData>
{
    private readonly List<Delegate?>? _delegates;
    private readonly List<Type>? _types;

    public static DeserializeRuntimeData None => default;

    internal IEnumerable<Delegate?> Delegates => _delegates ?? Enumerable.Empty<Delegate?>();
    internal IEnumerable<Type> Types => _types ?? Enumerable.Empty<Type>();

    internal DeserializeRuntimeData(List<Delegate?>? delegates, List<Type>? types)
    {
        _delegates = delegates;
        _types = types;
    }

    internal void Deconstruct(out List<Delegate?>? delegates, out int delegateIndex)
    {
        delegates = _delegates;
        delegateIndex = delegates?.Count ?? 0;
    }

    //internal EventSubscription<T> AddEventHandler<T>(Event<T> targetEvent, JsonElement handler)
    //{
    //    var key = handler.GetInt32();
    //    var d = GetDelegate(key);
    //    return d switch
    //    {
    //        Action<T> action => targetEvent.Subscribe(action),
    //        Action action => targetEvent.Subscribe(_ => action()),
    //        null => EventSubscription<T>.None,
    //        _ => throw new FormatException($"event handler type should be {typeof(Action<T>).FullName} or {typeof(Action).FullName}"),
    //    };
    //}

    internal Type? GetType(JsonElement propValue)
    {
        var key = propValue.GetInt32();
        return GetType(key);
    }

    internal T? GetDelegate<T>(JsonElement handler) where T : Delegate
    {
        var key = handler.GetInt32();
        return (T?)GetDelegate(key);
    }

    private Delegate? GetDelegate(int key)
    {
        var delegates = _delegates;
        if(delegates == null || (uint)key >= (uint)delegates.Count) {
            return null;
        }
        return delegates[key];
    }

    private Type? GetType(int key)
    {
        var types = _types;
        if(types == null || (uint)key >= (uint)types.Count) {
            return null;
        }
        return types[key];
    }

    public override bool Equals(object? obj) => obj is DeserializeRuntimeData data && Equals(data);

    public bool Equals(DeserializeRuntimeData other) => _delegates == other._delegates;

    public override int GetHashCode() => HashCode.Combine(_delegates);
}

public interface IReactive
{
    void ApplyDiff(in ReactSource source);
}
