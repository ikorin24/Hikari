#nullable enable
using Elffy.UI;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elffy;

public static class Serializer
{
    private sealed class ConstructorInfo
    {
        private readonly Type _type;

        // CtorFunc<T> (when T is struct)
        // CtorFuncCovariant<T> (when T is class)
        private readonly Delegate _func;

        // CtorFunc<object> (when T is struct)
        // null (when T is class)
        private readonly CtorFunc<ValueType>? _funcUntyped;

        public Type Type => _type;

        private ConstructorInfo(Type type, Delegate func, CtorFunc<ValueType>? funcUntyped)
        {
            _type = type;
            _func = func;
            _funcUntyped = funcUntyped;
        }

        public static ConstructorInfo New<T>(CtorFunc<T> func) where T : notnull
        {
            if(typeof(T).IsValueType) {
                var funcUntyped = new CtorFunc<ValueType>((in ReactSource x) =>
                {
                    return (ValueType)(object)func(x);
                });
                return new ConstructorInfo(typeof(T), func, funcUntyped);
            }
            else {
                var f = new CtorFuncCovariant<T>((in ReactSource s) => func(s));
                return new ConstructorInfo(typeof(T), f, null);
            }
        }

        public T Invoke<T>(in ReactSource source)
        {
            if(typeof(T).IsValueType) {

                if(typeof(T) == typeof(object) || typeof(T) == typeof(ValueType)) {
                    Debug.Assert(_funcUntyped is not null);
                    return (T)(object)_funcUntyped.Invoke(source);
                }
                else {
                    return ((CtorFunc<T>)_func).Invoke(source);
                }
            }
            else {
                return ((CtorFuncCovariant<T>)_func).Invoke(source);
            }
        }

        public object InvokeUntyped(in ReactSource source)
        {
            if(_type.IsValueType) {
                Debug.Assert(_funcUntyped is not null);
                return _funcUntyped.Invoke(source);
            }
            else {
                return ((CtorFuncCovariant<object>)_func).Invoke(source);
            }
        }

        private delegate T CtorFuncCovariant<out T>(in ReactSource source);
    }

    private static readonly ConcurrentDictionary<Type, ConstructorInfo> _constructorFuncs = new();

    internal static JsonDocumentOptions ParseOptions => new JsonDocumentOptions
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
        MaxDepth = 64,
    };

    private static readonly JsonSerializerOptions DefaultWriteSerializerOptions = new JsonSerializerOptions
    {
        AllowTrailingCommas = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    private static readonly JsonWriterOptions DefaultWriterOptions = new JsonWriterOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = true,
        MaxDepth = 1000,
        SkipValidation = false,
    };

    private static readonly JsonWriterOptions MinWriterOptions = new JsonWriterOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false,
        MaxDepth = 1000,
        SkipValidation = false,
    };

    static Serializer()
    {
        RegisterConstructor(ExternalConstructor.Color4FromJson);
    }

    public static void RegisterConstructor<T>(CtorFunc<T> constructoFunc) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(constructoFunc);
        var ctor = ConstructorInfo.New(constructoFunc);
        _constructorFuncs.TryAdd(ctor.Type, ctor);
    }

    public static string Serialize<T>(T value) where T : notnull, IToJson
    {
        var node = value.ToJson();
        return node?.ToJsonString(DefaultWriteSerializerOptions) ?? "null";
    }

    public static void Serialize<T>(T value, IBufferWriter<byte> bufferWriter) where T : notnull, IToJson
    {
        using var writer = new Utf8JsonWriter(bufferWriter, DefaultWriterOptions);
        var node = value.ToJson();
        if(node is null) {
            writer.WriteNullValue();
        }
        else {
            node.WriteTo(writer, DefaultWriteSerializerOptions);
        }
    }

    //public static T Deserialize<T>([StringSyntax(StringSyntaxAttribute.Json)] ReadOnlyMemory<byte> utf8Json, DeserializeRuntimeData data = default)
    //{
    //    using var doc = JsonDocument.Parse(utf8Json, ParseOptions);
    //    return DeserializeCore<T>(doc.RootElement, data);
    //}

    //public static T Deserialize<T>([StringSyntax(StringSyntaxAttribute.Json)] string json, DeserializeRuntimeData data = default)
    //{
    //    using var doc = JsonDocument.Parse(json, ParseOptions);
    //    return DeserializeCore<T>(doc.RootElement, data);
    //}

    //public static T Deserialize<T>([StringSyntax(StringSyntaxAttribute.Json)] ReadOnlyMemory<char> json, DeserializeRuntimeData data = default)
    //{
    //    using var doc = JsonDocument.Parse(json, ParseOptions);
    //    return DeserializeCore<T>(doc.RootElement, data);
    //}

    //internal static object Deserialize(string json, DeserializeRuntimeData data = default)
    //{
    //    using var doc = JsonDocument.Parse(json, ParseOptions);
    //    var rootElement = doc.RootElement;
    //    return rootElement.ValueKind switch
    //    {
    //        JsonValueKind.Object => GetConstructor(rootElement, null).Func(rootElement, data),
    //        _ => throw new FormatException("element should be kind of object"),
    //    };
    //}

    internal static T Instantiate<T>(in ReactSource source)
    {
        return DeserializeCore<T>(source);
    }

    //internal static object Instantiate(JsonElement element, DeserializeRuntimeData data)
    //{
    //    return DeserializeCore(element, null, data);
    //}

    public static object Instantiate(in ReactSource source, Type? type)
    {
        return DeserializeCore(source, type);
    }

    private static T DeserializeCore<T>(in ReactSource source)
    {
        if(typeof(T) == typeof(string)) { var x = source.GetStringNotNull(); return Unsafe.As<string, T>(ref x); }
        if(typeof(T) == typeof(bool)) { var x = source.GetBoolean(); return Unsafe.As<bool, T>(ref x); }
        if(typeof(T) == typeof(sbyte)) { var x = source.GetNumber<sbyte>(); return Unsafe.As<sbyte, T>(ref x); }
        if(typeof(T) == typeof(byte)) { var x = source.GetNumber<byte>(); return Unsafe.As<byte, T>(ref x); }
        if(typeof(T) == typeof(short)) { var x = source.GetNumber<short>(); return Unsafe.As<short, T>(ref x); }
        if(typeof(T) == typeof(ushort)) { var x = source.GetNumber<ushort>(); return Unsafe.As<ushort, T>(ref x); }
        if(typeof(T) == typeof(int)) { var x = source.GetNumber<int>(); return Unsafe.As<int, T>(ref x); }
        if(typeof(T) == typeof(uint)) { var x = source.GetNumber<uint>(); return Unsafe.As<uint, T>(ref x); }
        if(typeof(T) == typeof(long)) { var x = source.GetNumber<long>(); return Unsafe.As<long, T>(ref x); }
        if(typeof(T) == typeof(ulong)) { var x = source.GetNumber<ulong>(); return Unsafe.As<ulong, T>(ref x); }
        if(typeof(T) == typeof(float)) { var x = source.GetNumber<float>(); return Unsafe.As<float, T>(ref x); }
        if(typeof(T) == typeof(double)) { var x = source.GetNumber<double>(); return Unsafe.As<double, T>(ref x); }
        if(typeof(T) == typeof(decimal)) { var x = source.GetNumber<decimal>(); return Unsafe.As<decimal, T>(ref x); }
        if(typeof(T) == typeof(DateTime)) { var x = source.GetDateTime(); return Unsafe.As<DateTime, T>(ref x); }
        if(typeof(T) == typeof(DateTimeOffset)) { var x = source.GetDateTimeOffset(); return Unsafe.As<DateTimeOffset, T>(ref x); }
        if(typeof(T) == typeof(Guid)) { var x = source.GetGuid(); return Unsafe.As<Guid, T>(ref x); }

        if(typeof(T).IsEnum) {
            return (T)source.ToEnum(typeof(T));
        }
        if(typeof(T).IsAssignableTo(typeof(Delegate))) {
            var d = source.RestoreDelegate<Delegate>() ?? throw new FormatException("null");
            return (T)(object)d;
        }

        if(typeof(T) == typeof(string[])) {
            var array = new string[source.GetArrayLength()];
            var i = 0;
            foreach(var item in source.EnumerateArray()) {
                array[i++] = item.GetStringNotNull();
            }
            return Unsafe.As<string[], T>(ref array);
        }
        if(typeof(T) == typeof(int[])) {
            var array = new int[source.GetArrayLength()];
            var i = 0;
            foreach(var item in source.EnumerateArray()) {
                array[i++] = item.GetNumber<int>();
            }
            return Unsafe.As<int[], T>(ref array);
        }
        if(typeof(T) == typeof(float[])) {
            var array = new float[source.GetArrayLength()];
            var i = 0;
            foreach(var item in source.EnumerateArray()) {
                array[i++] = item.GetNumber<float>();
            }
            return Unsafe.As<float[], T>(ref array);
        }

        switch(source.ValueKind) {
            case JsonValueKind.Array: {
                var arrayType = typeof(T);
                return (T)CreateArray(source, arrayType);
            }
            case JsonValueKind.Object: {
                var type = source.ObjectType ?? throw new ArgumentException($"cannot determine type.\n{source.ToDebugString()}");
                var ctor = FindCtor(type);
                return ctor.Invoke<T>(source);
            }
            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null: {
                var type = typeof(T);
                var ctor = FindCtor(type);
                return ctor.Invoke<T>(source);
            }
            case JsonValueKind.Undefined:
            default: {
                throw new FormatException("undefined");
            }
        }
    }

    private static object DeserializeCore(in ReactSource source, Type? leftSideType)
    {
        if(leftSideType == typeof(string)) { return source.GetStringNotNull(); }
        if(leftSideType == typeof(bool)) { return source.GetBoolean(); }
        if(leftSideType == typeof(sbyte)) { return source.GetNumber<sbyte>(); }
        if(leftSideType == typeof(byte)) { return source.GetNumber<byte>(); }
        if(leftSideType == typeof(short)) { return source.GetNumber<short>(); }
        if(leftSideType == typeof(ushort)) { return source.GetNumber<ushort>(); }
        if(leftSideType == typeof(int)) { return source.GetNumber<int>(); }
        if(leftSideType == typeof(uint)) { return source.GetNumber<uint>(); }
        if(leftSideType == typeof(long)) { return source.GetNumber<long>(); }
        if(leftSideType == typeof(ulong)) { return source.GetNumber<ulong>(); }
        if(leftSideType == typeof(float)) { return source.GetNumber<float>(); }
        if(leftSideType == typeof(double)) { return source.GetNumber<double>(); }
        if(leftSideType == typeof(decimal)) { return source.GetNumber<decimal>(); }
        if(leftSideType == typeof(DateTime)) { return source.GetDateTime(); }
        if(leftSideType == typeof(DateTimeOffset)) { return source.GetDateTimeOffset(); }
        if(leftSideType == typeof(Guid)) { return source.GetGuid(); }

        if(leftSideType?.IsEnum == true) {
            return source.ToEnum(leftSideType);
        }
        if(leftSideType?.IsAssignableTo(typeof(Delegate)) == true) {
            var d = source.RestoreDelegate<Delegate>() ?? throw new FormatException("null");
            return d;
        }

        if(leftSideType == typeof(string[])) {
            var array = new string[source.GetArrayLength()];
            var i = 0;
            foreach(var item in source.EnumerateArray()) {
                array[i++] = item.GetStringNotNull();
            }
            return array;
        }
        if(leftSideType == typeof(int[])) {
            var array = new int[source.GetArrayLength()];
            var i = 0;
            foreach(var item in source.EnumerateArray()) {
                array[i++] = item.GetNumber<int>();
            }
            return array;
        }
        if(leftSideType == typeof(float[])) {
            var array = new float[source.GetArrayLength()];
            var i = 0;
            foreach(var item in source.EnumerateArray()) {
                array[i++] = item.GetNumber<float>();
            }
            return array;
        }

        switch(source.ValueKind) {
            case JsonValueKind.Array: {
                var arrayType = leftSideType ?? throw new ArgumentException($"cannot determine type.\n{source.ToDebugString()}");
                return CreateArray(source, arrayType);
            }
            case JsonValueKind.Object: {
                var type = source.ObjectType ?? throw new ArgumentException($"cannot determine type.\n{source.ToDebugString()}");
                var ctor = FindCtor(type);
                return ctor.InvokeUntyped(source);
            }
            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null: {
                var type = leftSideType ?? throw new ArgumentException($"cannot determine type.\n{source.ToDebugString()}");
                var ctor = FindCtor(type);
                return ctor.InvokeUntyped(source);
            }
            case JsonValueKind.Undefined:
            default: {
                throw new FormatException("undefined");
            }
        }
    }

    private static object CreateArray(in ReactSource source, Type leftSideType)
    {
        Debug.Assert(source.ValueKind == JsonValueKind.Array);
        if(leftSideType.IsSZArray) {
            var childType = leftSideType.GetElementType() ?? throw new UnreachableException();
            var array = Array.CreateInstance(childType, source.GetArrayLength());
            var i = 0;
            foreach(var item in source.EnumerateArray()) {
                array.SetValue(DeserializeCore(item, childType), i);
                i++;
            }
            return array;
        }
        if(TryGetCtorFromType(leftSideType, out var ctor)) {
            return ctor.InvokeUntyped(source);
        }
        throw new FormatException($"type \"{leftSideType.FullName}\" cannot be created from json");
    }

    private static ConstructorInfo FindCtor(Type type)
    {
        if(TryGetCtorFromType(type, out var ctor) == false) {
            throw new ArgumentException($"constructor is not found. Type: {type.FullName}");
        }
        return ctor;
    }

    private static bool TryGetCtorFromType(Type type, [MaybeNullWhen(false)] out ConstructorInfo ctor)
    {
        if(_constructorFuncs.TryGetValue(type, out ctor)) {
            return true;
        }
        RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        if(_constructorFuncs.TryGetValue(type, out ctor)) {
            return true;
        }
        ctor = null;
        return false;
    }
}

internal static class ExternalConstructor
{
    public static Color4 Color4FromJson(in ReactSource source)
    {
        switch(source.ValueKind) {
            case JsonValueKind.String: {
                var str = source.GetStringNotNull();
                if(Color4.TryFromHexCode(str, out var color) || Color4.TryFromWebColorName(str, out color)) {
                    return color;
                }
                break;
            }
            case JsonValueKind.Object: {
                if(source.HasObjectType(out var type) && type == typeof(Color4)) {
                    var color = new Color4();
                    if(source.TryGetProperty(nameof(Color4.R), out var r)) {
                        color.R = r.GetNumber<float>();
                    }
                    if(source.TryGetProperty(nameof(Color4.G), out var g)) {
                        color.G = g.GetNumber<float>();
                    }
                    if(source.TryGetProperty(nameof(Color4.B), out var b)) {
                        color.B = b.GetNumber<float>();
                    }
                    if(source.TryGetProperty(nameof(Color4.A), out var a)) {
                        color.A = a.GetNumber<float>();
                    }
                    return color;
                }
                break;
            }
            default:
                break;
        }
        source.ThrowInvalidFormat();
        return default;
    }

    public static JsonNode ToJson(this Color4 self)
    {
        return new JsonObject
        {
            ["@type"] = typeof(Color4).FullName,
            [nameof(Color4.R)] = self.R,
            [nameof(Color4.G)] = self.G,
            [nameof(Color4.B)] = self.B,
            [nameof(Color4.A)] = self.A
        };
    }
}

public interface IFromJson<TSelf>
    where TSelf : IFromJson<TSelf>
{
    abstract static TSelf FromJson(in ReactSource source);
}

public interface IToJson
{
    JsonNode? ToJson();
}

internal static class EnumJsonHelper
{
    public static JsonValue ToJson<T>(this T self) where T : struct, Enum
    {
        var str = self.ToString();
        return JsonValue.Create(str)!;
    }

    private static string GetStringNotNull(this JsonElement element)
    {
        var str = element.GetString();
        if(str == null) {
            Throw();
        }
        return str;

        [DoesNotReturn]
        static void Throw() => throw new FormatException("null");
    }
}

// [NOTE] no covariant
public delegate T CtorFunc<T>(in ReactSource source);
