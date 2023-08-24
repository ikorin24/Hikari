#nullable enable
using Hikari;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Hikari.UI;

public readonly partial struct ObjectSource : IEquatable<ObjectSource>
{
    private readonly JsonElement _element;
    private readonly List<Delegate>? _delegates;
    private readonly List<Type>? _types;

    public static ObjectSource Empty => default;

    public bool IsEmpty => JsonElementEqualityComparer.Equals(_element, default);

    internal JsonElement Element => _element;

    public JsonValueKind ValueKind => _element.ValueKind;

    public bool IsArray => _element.ValueKind == JsonValueKind.Array;
    public bool IsObject => _element.ValueKind == JsonValueKind.Object;

    public Type? ObjectType => HasObjectType(out var type) ? type : null;

    public string? ObjectKey => HasObjectKey(out var key) ? key : null;

    internal ObjectSource(string str, List<Delegate>? delegates, List<Type>? types)
    {
        // Clone the root element to make the lifetime permanent.
        using(var json = JsonDocument.Parse(str, Serializer.ParseOptions)) {
            _element = json.RootElement.Clone();
        }
        _delegates = delegates;
        _types = types;
    }

    private ObjectSource(JsonElement element, List<Delegate>? delegates, List<Type>? types)
    {
        _element = element;
        _delegates = delegates;
        _types = types;
    }

    public bool HasObjectType([MaybeNullWhen(false)] out Type type)
    {
        type = _element.ValueKind switch
        {
            JsonValueKind.Object => _element.TryGetProperty("@type"u8, out var typeProp) switch
            {
                true => typeProp.ValueKind switch
                {
                    JsonValueKind.String => GetTypeFromList(typeProp.GetString(), _types),
                    _ => null,
                },
                false => null,
            },
            _ => null,
        };
        return type != null;
    }

    public bool HasObjectKey([MaybeNullWhen(false)] out string key)
    {
        key = _element.ValueKind switch
        {
            JsonValueKind.Object => _element.TryGetProperty("@key"u8, out var keyProp) switch
            {
                true => keyProp.ValueKind switch
                {
                    JsonValueKind.String => keyProp.GetString(),
                    _ => null,
                },
                false => null,
            },
            _ => null,
        };
        return key != null;
    }

    public bool TryGetProperty(string propertyName, out ObjectSource value)
    {
        if(_element.TryGetProperty(propertyName, out var prop)) {
            value = new ObjectSource(prop, _delegates, _types);
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

    public T ApplyProperty<T>(string propName, in T current, Func<T> defaultFactory, out (ApplySourceResult Result, T? Old) output) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(defaultFactory);
        if(TryGetProperty(propName, out var propValue)) {
            return propValue.Apply(current, out output);
        }
        output = (ApplySourceResult.InstanceReplaced, current);
        return defaultFactory.Invoke();
    }

    public T Apply<T>(in T? current, out (ApplySourceResult Result, T? Old) output) where T : notnull
    {
        if(current is IReactive reactive) {
            if(HasObjectType(out var type) && type == current.GetType()) {
                reactive.ApplyDiff(this);
                output = (ApplySourceResult.PropertyDiffApplied, current);
                return current;
            }
            else if(IsArray) {
                reactive.ApplyDiff(this);
                output = (ApplySourceResult.ArrayDiffApplied, current);
                return current;
            }
        }
        output = (ApplySourceResult.InstanceReplaced, current);
        return Instantiate<T>();
    }

    public void ApplyDiff<T>(T reactive) where T : class, IReactive
    {
        ArgumentNullException.ThrowIfNull(reactive);
        if(HasObjectType(out var type) && type == reactive.GetType()) {
            reactive.ApplyDiff(this);
        }
        else if(IsArray) {
            reactive.ApplyDiff(this);
        }
        else {
            throw new InvalidOperationException($"the source cannot be applied to the specified target.(target = {reactive})\n{ToDebugString()}");
        }
    }

    public int GetArrayLength() => _element.GetArrayLength();

    public ArrayEnumerable EnumerateArray() => new ArrayEnumerable(this);

    public PropertyEnumerable EnumerateProperties() => new PropertyEnumerable(this);

    public T Instantiate<T>()
    {
        return Serializer.Instantiate<T>(this);
    }

    public object Instantiate(Type? type)
    {
        return Serializer.Instantiate(this, type);
    }

    internal T? RestoreDelegate<T>() where T : Delegate
    {
        var d = GetDelegateFromList(_element.GetString(), _delegates);
        return (T?)d;
    }

    [GeneratedRegex(@"^(?<i>\d+)@types$")]
    private static partial Regex TypeRefDataRegex();

    [GeneratedRegex(@"^(?<i>\d+)@delegates$")]
    private static partial Regex DelegateRefDataRegex();

    private static Type? GetTypeFromList(string? value, List<Type>? types)
    {
        return FromList(value, TypeRefDataRegex(), types);
    }

    private static Delegate? GetDelegateFromList(string? value, List<Delegate>? delegates)
    {
        return FromList(value, DelegateRefDataRegex(), delegates);
    }

    private static T? FromList<T>(string? value, Regex regex, List<T>? list) where T : class
    {
        if(value == null || list == null) {
            return null;
        }
        var match = regex.Match(value);
        if(match == null) {
            return null;
        }
        if(int.TryParse(match.Groups["i"].ValueSpan, out var index) == false) {
            return null;
        }
        if((uint)index >= (uint)list.Count) {
            return null;
        }
        return list[index];
    }

    private Type? GetObjectType(int key)
    {
        var types = _types;
        if(types == null || (uint)key >= (uint)types.Count) {
            return null;
        }
        return types[key];
    }

    [DoesNotReturn]
    public void ThrowInvalidFormat() => throw new FormatException(ToDebugString());

    public string ToDebugString()
    {
        var delegates = _delegates;
        var types = _types;

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
                if(delegates != null) {
                    foreach(var d in delegates) {
                        writer.WriteStringValue(d?.Method?.Name);
                    }
                }
                writer.WriteEndArray();
                if(types != null) {
                    writer.WriteStartArray("types");
                    foreach(var t in types) {
                        writer.WriteStringValue(t.FullName);
                    }
                    writer.WriteEndArray();
                }
            }
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
        writer.Flush();
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public override bool Equals(object? obj) => obj is ObjectSource source && Equals(source);

    public bool Equals(ObjectSource other)
    {
        return
            JsonElementEqualityComparer.Equals(_element, other._element) &&
            EqualityComparer<List<Delegate>?>.Default.Equals(_delegates, other._delegates) &&
            EqualityComparer<List<Type>?>.Default.Equals(_types, other._types);
    }

    public override int GetHashCode() => HashCode.Combine(_element, _delegates, _types);

    public readonly record struct ArrayEnumerable(in ObjectSource Source) : IEnumerable<ObjectSource>
    {
        public ArrayEnumerator GetEnumerator() => new ArrayEnumerator(Source);

        IEnumerator<ObjectSource> IEnumerable<ObjectSource>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct ArrayEnumerator : IEnumerator<ObjectSource>
    {
        private readonly List<Delegate>? _delegates;
        private readonly List<Type>? _types;
        private JsonElement.ArrayEnumerator _enumerator;

        internal ArrayEnumerator(in ObjectSource source)
        {
            _delegates = source._delegates;
            _types = source._types;
            _enumerator = source.Element.EnumerateArray();
        }

        public ObjectSource Current => new ObjectSource(_enumerator.Current, _delegates, _types);

        object IEnumerator.Current => Current;

        public void Dispose() => _enumerator.Dispose();

        public bool MoveNext() => _enumerator.MoveNext();

        public void Reset() => _enumerator.Reset();
    }

    public readonly record struct PropertyEnumerable(in ObjectSource Source) : IEnumerable<ObjectSourceProperty>
    {
        public PropertyEnumerator GetEnumerator() => new PropertyEnumerator(Source);

        IEnumerator<ObjectSourceProperty> IEnumerable<ObjectSourceProperty>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct PropertyEnumerator : IEnumerator<ObjectSourceProperty>
    {
        private readonly List<Delegate>? _delegates;
        private readonly List<Type>? _types;
        private JsonElement.ObjectEnumerator _enumerator;

        internal PropertyEnumerator(in ObjectSource source)
        {
            _delegates = source._delegates;
            _types = source._types;
            _enumerator = source.Element.EnumerateObject();
        }

        public ObjectSourceProperty Current
        {
            get
            {
                var current = _enumerator.Current;
                return new ObjectSourceProperty
                {
                    Name = current.Name,
                    Value = new ObjectSource(current.Value, _delegates, _types),
                };
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose() => _enumerator.Dispose();

        public bool MoveNext() => _enumerator.MoveNext();

        public void Reset() => _enumerator.Reset();
    }

    public static bool operator ==(ObjectSource left, ObjectSource right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ObjectSource left, ObjectSource right)
    {
        return !(left == right);
    }
}

public readonly record struct ObjectSourceProperty(string Name, ObjectSource Value);

public enum ApplySourceResult
{
    InstanceReplaced = 0,
    PropertyDiffApplied = 1,
    ArrayDiffApplied = 2,
}

file static class JsonElementEqualityComparer
{
    private static readonly bool _canUseUnsafePath;
    private static readonly FieldInfo[] _fields;

    static JsonElementEqualityComparer()
    {
        _canUseUnsafePath = CheckUnsafePathAvailable(out _fields);
    }

    private static bool CheckUnsafePathAvailable(out FieldInfo[] fields)
    {
        fields = typeof(JsonElement).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if(Unsafe.SizeOf<JsonElement>() != Unsafe.SizeOf<JsonElementDummy>()) {
            return false;
        }
        return
            fields.Length == 2 &&
            fields[0].FieldType == typeof(JsonDocument) &&
            fields[1].FieldType == typeof(int);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(JsonElement a, JsonElement b)
    {
        // JsonElement does not implement IEquatable<JsonElement>,
        // so I compare the fields by unsafe way.
        // If the way is not available, fallback to reflection.

        if(_canUseUnsafePath) {
            ref var a1 = ref Unsafe.As<JsonElement, JsonElementDummy>(ref a);
            ref var b1 = ref Unsafe.As<JsonElement, JsonElementDummy>(ref b);
            return
                a1._parent == b1._parent &&
                a1._idx == b1._idx;
        }
        else {
            return EqualsFallback(a, b);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool EqualsFallback(JsonElement a, JsonElement b)
    {
        // That is same way as ValueType.Equals(object), but more faster because FieldInfo is cached.

        for(int i = 0; i < _fields.Length; i++) {
            object? v1 = _fields[i].GetValue(a);
            object? v2 = _fields[i].GetValue(b);

            if(v1 == null) {
                if(v2 != null) {
                    return false;
                }
            }
            else if(!v1.Equals(v2)) {
                return false;
            }
        }
        return true;
    }

    private readonly struct JsonElementDummy
    {
        public readonly JsonDocument _parent;
        public readonly int _idx;
    }
}
