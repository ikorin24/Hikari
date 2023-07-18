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
    private record ConstructorInfo(Type Type, ConstructorFunc Func);

    private static readonly ConcurrentDictionary<string, ConstructorInfo> _constructorFuncs = new();
    private static readonly ConcurrentDictionary<string, Type> _shortNames = new()
    {
        ["object"] = typeof(object),
        ["button"] = typeof(Button),
        ["panel"] = typeof(Panel),
    };

    private static JsonDocumentOptions ParseOptions => new JsonDocumentOptions
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

    public static void RegisterConstructor<T>(ConstructorFunc<T> constructoFunc) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(constructoFunc);
        var f = new ConstructorFunc((JsonElement e, in DeserializeRuntimeData d) => constructoFunc(e, d));
        var value = new ConstructorInfo(typeof(T), f);
        _constructorFuncs.TryAdd(typeof(T).FullName!, value);
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

    public static T Deserialize<T>([StringSyntax(StringSyntaxAttribute.Json)] ReadOnlyMemory<byte> utf8Json, DeserializeRuntimeData data = default)
    {
        using var doc = JsonDocument.Parse(utf8Json, ParseOptions);
        return DeserializeCore<T>(doc.RootElement, data);
    }

    public static T Deserialize<T>([StringSyntax(StringSyntaxAttribute.Json)] string json, DeserializeRuntimeData data = default)
    {
        using var doc = JsonDocument.Parse(json, ParseOptions);
        return DeserializeCore<T>(doc.RootElement, data);
    }

    public static T Deserialize<T>([StringSyntax(StringSyntaxAttribute.Json)] ReadOnlyMemory<char> json, DeserializeRuntimeData data = default)
    {
        using var doc = JsonDocument.Parse(json, ParseOptions);
        return DeserializeCore<T>(doc.RootElement, data);
    }

    internal static object Deserialize(string json, DeserializeRuntimeData data = default)
    {
        using var doc = JsonDocument.Parse(json, ParseOptions);
        var rootElement = doc.RootElement;
        return rootElement.ValueKind switch
        {
            JsonValueKind.Object => GetConstructor(rootElement, null).Func(rootElement, data),
            _ => throw new FormatException("element should be kind of object"),
        };
    }

    public static T Instantiate<T>(JsonElement element, DeserializeRuntimeData data = default)
    {
        return DeserializeCore<T>(element, data);
    }

    public static object Instantiate(JsonElement element, DeserializeRuntimeData data = default)
    {
        return DeserializeCore(element, null, data);
    }

    public static object Instantiate(JsonElement element, Type type, DeserializeRuntimeData data = default)
    {
        ArgumentNullException.ThrowIfNull(type);
        return DeserializeCore(element, type, data);
    }

    private static T DeserializeCore<T>(JsonElement element, DeserializeRuntimeData data)
    {
        if(typeof(T) == typeof(string)) { var x = element.GetStringNotNull(); return Unsafe.As<string, T>(ref x); }
        if(typeof(T) == typeof(bool)) { var x = element.GetBoolean(); return Unsafe.As<bool, T>(ref x); }
        if(typeof(T) == typeof(sbyte)) { var x = element.GetSByte(); return Unsafe.As<sbyte, T>(ref x); }
        if(typeof(T) == typeof(byte)) { var x = element.GetByte(); return Unsafe.As<byte, T>(ref x); }
        if(typeof(T) == typeof(short)) { var x = element.GetInt16(); return Unsafe.As<short, T>(ref x); }
        if(typeof(T) == typeof(ushort)) { var x = element.GetUInt16(); return Unsafe.As<ushort, T>(ref x); }
        if(typeof(T) == typeof(int)) { var x = element.GetInt32(); return Unsafe.As<int, T>(ref x); }
        if(typeof(T) == typeof(uint)) { var x = element.GetUInt32(); return Unsafe.As<uint, T>(ref x); }
        if(typeof(T) == typeof(long)) { var x = element.GetInt64(); return Unsafe.As<long, T>(ref x); }
        if(typeof(T) == typeof(ulong)) { var x = element.GetUInt64(); return Unsafe.As<ulong, T>(ref x); }
        if(typeof(T) == typeof(float)) { var x = element.GetSingle(); return Unsafe.As<float, T>(ref x); }
        if(typeof(T) == typeof(double)) { var x = element.GetDouble(); return Unsafe.As<double, T>(ref x); }
        if(typeof(T) == typeof(decimal)) { var x = element.GetDecimal(); return Unsafe.As<decimal, T>(ref x); }
        if(typeof(T) == typeof(DateTime)) { var x = element.GetDateTime(); return Unsafe.As<DateTime, T>(ref x); }
        if(typeof(T) == typeof(DateTimeOffset)) { var x = element.GetDateTimeOffset(); return Unsafe.As<DateTimeOffset, T>(ref x); }
        if(typeof(T) == typeof(Guid)) { var x = element.GetGuid(); return Unsafe.As<Guid, T>(ref x); }

        if(typeof(T).IsEnum) {
            return (T)element.ToEnum(typeof(T));
        }

        if(typeof(T) == typeof(string[])) {
            var array = new string[element.GetArrayLength()];
            var i = 0;
            foreach(var item in element.EnumerateArray()) {
                array[i++] = item.GetStringNotNull();
            }
            return Unsafe.As<string[], T>(ref array);
        }
        if(typeof(T) == typeof(int[])) {
            var array = new int[element.GetArrayLength()];
            var i = 0;
            foreach(var item in element.EnumerateArray()) {
                array[i++] = item.GetInt32();
            }
            return Unsafe.As<int[], T>(ref array);
        }
        if(typeof(T) == typeof(float[])) {
            var array = new float[element.GetArrayLength()];
            var i = 0;
            foreach(var item in element.EnumerateArray()) {
                array[i++] = item.GetSingle();
            }
            return Unsafe.As<float[], T>(ref array);
        }

        return element.ValueKind switch
        {
            JsonValueKind.Array => (T)CreateArray(element, typeof(T), data),
            JsonValueKind.Object or
            JsonValueKind.String or
            JsonValueKind.Number or
            JsonValueKind.Null or
            JsonValueKind.True or
            JsonValueKind.False => (T)GetConstructor(element, typeof(T)).Func(element, data),
            JsonValueKind.Undefined or _ => throw new FormatException("undefined"),
        };
    }

    private static object DeserializeCore(JsonElement element, Type? leftSideType, DeserializeRuntimeData data)
    {
        if(leftSideType == typeof(string)) { return element.GetStringNotNull(); }
        if(leftSideType == typeof(bool)) { return element.GetBoolean(); }
        if(leftSideType == typeof(sbyte)) { return element.GetSByte(); }
        if(leftSideType == typeof(byte)) { return element.GetByte(); }
        if(leftSideType == typeof(short)) { return element.GetInt16(); }
        if(leftSideType == typeof(ushort)) { return element.GetUInt16(); }
        if(leftSideType == typeof(int)) { return element.GetInt32(); }
        if(leftSideType == typeof(uint)) { return element.GetUInt32(); }
        if(leftSideType == typeof(long)) { return element.GetInt64(); }
        if(leftSideType == typeof(ulong)) { return element.GetUInt64(); }
        if(leftSideType == typeof(float)) { return element.GetSingle(); }
        if(leftSideType == typeof(double)) { return element.GetDouble(); }
        if(leftSideType == typeof(decimal)) { return element.GetDecimal(); }
        if(leftSideType == typeof(DateTime)) { return element.GetDateTime(); }
        if(leftSideType == typeof(DateTimeOffset)) { return element.GetDateTimeOffset(); }
        if(leftSideType == typeof(Guid)) { return element.GetGuid(); }

        if(leftSideType?.IsEnum == true) {
            return element.ToEnum(leftSideType);
        }

        if(leftSideType == typeof(string[])) {
            var array = new string[element.GetArrayLength()];
            var i = 0;
            foreach(var item in element.EnumerateArray()) {
                array[i++] = item.GetStringNotNull();
            }
            return array;
        }
        if(leftSideType == typeof(int[])) {
            var array = new int[element.GetArrayLength()];
            var i = 0;
            foreach(var item in element.EnumerateArray()) {
                array[i++] = item.GetInt32();
            }
            return array;
        }
        if(leftSideType == typeof(float[])) {
            var array = new float[element.GetArrayLength()];
            var i = 0;
            foreach(var item in element.EnumerateArray()) {
                array[i++] = item.GetSingle();
            }
            return array;
        }

        return element.ValueKind switch
        {
            JsonValueKind.Array => CreateArray(
                element,
                leftSideType ?? throw new ArgumentException($"cannot determine type: {element}"),
                data),
            JsonValueKind.Object or
            JsonValueKind.String or
            JsonValueKind.Number or
            JsonValueKind.Null or
            JsonValueKind.True or
            JsonValueKind.False => GetConstructor(element, leftSideType).Func(element, data),
            JsonValueKind.Undefined or _ => throw new FormatException("undefined"),
        };
    }

    private static ConstructorInfo GetConstructor(JsonElement element, Type? leftSideType)
    {
        Debug.Assert(element.ValueKind is
            JsonValueKind.Object or
            JsonValueKind.String or
            JsonValueKind.Number or
            JsonValueKind.Null or
            JsonValueKind.True or
            JsonValueKind.False);

        switch(element.ValueKind) {
            case JsonValueKind.Object: {
                var typename = element.GetProperty("@type"u8).GetStringNotNull();
                ConstructorInfo? ctor;
                if(_shortNames.TryGetValue(typename, out var type)) {
                    if(FindCtorFromType(type, out ctor) == false) {
                        throw new FormatException($"type \"{typename}\" cannot be created from json");
                    }
                }
                else {
                    if(FindCtorFromName(typename, out ctor) == false) {
                        throw new FormatException($"type \"{typename}\" cannot be created from json");
                    }
                }
                if(leftSideType != null) {
                    if(ctor.Type.IsAssignableTo(leftSideType) == false) {
                        throw new FormatException($"{ctor.Type.FullName} is not assignable to {leftSideType.FullName}");
                    }
                }
                return ctor;
            }
            default: {
                if(leftSideType == null) {
                    throw new ArgumentException($"cannot determine type: {element}");
                }
                var typename = leftSideType.FullName ?? throw new ArgumentException();
                if(FindCtorFromName(typename, out var ctor) == false) {
                    throw new FormatException($"type \"{typename}\" cannot be created from json");
                }
                if(ctor.Type.IsAssignableTo(leftSideType) == false) {
                    throw new FormatException($"{ctor.Type.FullName} is not assignable to {leftSideType.FullName}");
                }
                return ctor;
            }
        }
    }

    private static object CreateArray(JsonElement element, Type leftSideType, DeserializeRuntimeData data)
    {
        Debug.Assert(element.ValueKind == JsonValueKind.Array);
        if(leftSideType.IsSZArray) {
            var childType = leftSideType.GetElementType() ?? throw new UnreachableException();
            var array = Array.CreateInstance(childType, element.GetArrayLength());
            var i = 0;
            foreach(var item in element.EnumerateArray()) {
                array.SetValue(DeserializeCore(item, childType, data), i);
                i++;
            }
            return array;
        }
        if(FindCtorFromType(leftSideType, out var ctor)) {
            return ctor.Func(element, DeserializeRuntimeData.None);
        }
        throw new FormatException($"type \"{leftSideType.FullName}\" cannot be created from json");
    }

    private static bool FindCtorFromType(Type type, [MaybeNullWhen(false)] out ConstructorInfo ctor)
    {
        if(_constructorFuncs.TryGetValue(type.FullName!, out ctor)) {
            return true;
        }
        RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        if(_constructorFuncs.TryGetValue(type.FullName!, out ctor)) {
            return true;
        }
        ctor = null;
        return false;
    }

    private static bool FindCtorFromName(string typeFullName, [MaybeNullWhen(false)] out ConstructorInfo ctor)
    {
        if(_constructorFuncs.TryGetValue(typeFullName, out ctor)) {
            return true;
        }
        var type = Type.GetType(typeFullName);
        if(type != null) {
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            if(_constructorFuncs.TryGetValue(typeFullName, out ctor)) {
                return true;
            }
        }
        return false;
    }
}

internal static class ExternalConstructor
{
    public static Color4 Color4FromJson(JsonElement element, in DeserializeRuntimeData data)
    {
        switch(element.ValueKind) {
            case JsonValueKind.String: {
                var str = element.GetStringNotNull();
                if(Color4.TryFromHexCode(str, out var color) || Color4.TryFromWebColorName(str, out color)) {
                    return color;
                }
                break;
            }
            case JsonValueKind.Object: {
                if(element.GetProperty("@type"u8).GetStringNotNull() == typeof(Color4).FullName) {
                    var color = new Color4();
                    if(element.TryGetProperty("r", out var r)) {
                        color.R = r.GetSingle();
                    }
                    if(element.TryGetProperty("g", out var g)) {
                        color.G = g.GetSingle();
                    }
                    if(element.TryGetProperty("b", out var b)) {
                        color.B = b.GetSingle();
                    }
                    if(element.TryGetProperty("a", out var a)) {
                        color.A = a.GetSingle();
                    }
                    return color;
                }
                break;
            }
            default:
                break;
        }
        throw new FormatException($"invalid format: {element}");
    }

    public static JsonNode ToJson(this Color4 self)
    {
        return new JsonObject
        {
            ["@type"] = typeof(Color4).FullName,
            ["r"] = self.R,
            ["g"] = self.G,
            ["b"] = self.B,
            ["a"] = self.A
        };
    }
}

public interface IFromJson<TSelf>
    where TSelf : IFromJson<TSelf>
{
    abstract static TSelf FromJson(JsonElement element, in DeserializeRuntimeData data);
}

public interface IToJson
{
    JsonNode? ToJson();
}

public static class EnumJsonHelper
{
    public static JsonValue ToJson<T>(this T self) where T : struct, Enum
    {
        var str = self.ToString();
        return JsonValue.Create(str)!;
    }

    public static T ToEnum<T>(this JsonElement self) where T : struct, Enum
    {
        return Enum.Parse<T>(self.GetStringNotNull());
    }

    public static object ToEnum(this JsonElement self, Type type)
    {
        return Enum.Parse(type, self.GetStringNotNull());
    }
}

internal static class JsonElementExtensions
{
    public static string GetStringNotNull(this JsonElement element)
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

public delegate object ConstructorFunc(JsonElement element, in DeserializeRuntimeData data);
public delegate T ConstructorFunc<out T>(JsonElement element, in DeserializeRuntimeData data);
