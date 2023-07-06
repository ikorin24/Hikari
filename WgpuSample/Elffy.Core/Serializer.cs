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
    private record ConstructorFunc(Type Type, Func<JsonNode?, object> Func);

    private static readonly ConcurrentDictionary<string, ConstructorFunc> _constructorFuncs = new();
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
        RegisterConstructor(node =>
        {
            ArgumentNullException.ThrowIfNull(node);
            return new object();
        });
    }

    public static void RegisterConstructor<T>(Func<JsonNode?, T> constructoFunc) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(constructoFunc);
        Func<JsonNode?, object> f = arg => constructoFunc(arg);
        var value = new ConstructorFunc(typeof(T), f);
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

    public static T Deserialize<T>([StringSyntax(StringSyntaxAttribute.Json)] ReadOnlySpan<byte> utf8Json) where T : notnull
    {
        var node = JsonNode.Parse(utf8Json, documentOptions: ParseOptions) ?? throw new FormatException("null");
        return (T)ParseJsonNode(node, typeof(T));
    }

    public static T Deserialize<T>([StringSyntax(StringSyntaxAttribute.Json)] string json) where T : notnull
    {
        var node = JsonNode.Parse(json, documentOptions: ParseOptions) ?? throw new FormatException("null");
        return (T)ParseJsonNode(node, typeof(T));
    }

    public static T Instantiate<T>(JsonNode? node) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(node);
        return (T)ParseJsonNode(node, typeof(T));
    }


    [return: NotNullIfNotNull(nameof(node))]
    private static object? ParseJsonNode(JsonNode? node, Type leftSideType)
    {
        return node switch
        {
            JsonArray array => ParseJsonArray(array, leftSideType),
            JsonObject obj => ParseJsonObject(obj, leftSideType),
            JsonValue value => ParseJsonValue(value, leftSideType),
            null => null,
            _ => throw new NotSupportedException(),
        };
    }

    private static object ParseJsonObject(JsonObject obj, Type leftSideType)
    {
        obj.GetPropOrThrow("@type", out string typename);
        ConstructorFunc? ctor;
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
        if(ctor.Type.IsAssignableTo(leftSideType) == false) {
            throw new FormatException($"{ctor.Type.FullName} is not assignable to {leftSideType.FullName}");
        }
        return ctor.Func(obj);
    }

    private static object ParseJsonValue(JsonValue value, Type leftSideType)
    {
        if(FindCtorFromType(leftSideType, out var ctor) == false) {
            throw new FormatException($"type \"{leftSideType.FullName}\" cannot be created from json");
        }
        return ctor.Func(value);
    }

    private static object ParseJsonArray(JsonArray array, Type leftSideType)
    {
        if(FindCtorFromType(leftSideType, out var ctor)) {
            return ctor.Func(array);
        }

        if(leftSideType.IsSZArray) {
            var elementType = leftSideType.GetElementType() ?? throw new UnreachableException();
            var a = Array.CreateInstance(elementType, array.Count);
            for(int i = 0; i < a.Length; i++) {
                a.SetValue(ParseJsonNode(array[i], elementType), i);
            }
            return a;
        }
        throw new FormatException($"type \"{leftSideType.FullName}\" cannot be created from json");
    }

    private static bool FindCtorFromType(Type type, [MaybeNullWhen(false)] out ConstructorFunc ctor)
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

    private static bool FindCtorFromName(string typeFullName, [MaybeNullWhen(false)] out ConstructorFunc ctor)
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

public interface IFromJson<TSelf>
{
    abstract static TSelf FromJson(JsonNode? node);
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

    public static T ToEnum<T>(this JsonNode self) where T : struct, Enum
    {
        var str = self.AsValue().GetValue<string>();
        return Enum.Parse<T>(str);
    }
}

internal static class JsonObjectExtensions
{
    public static bool GetProp(this JsonObject obj, string propname, ref sbyte value) => GetPropCore(obj, propname, ref value);
    public static bool GetProp(this JsonObject obj, string propname, ref byte value) => GetPropCore(obj, propname, ref value);
    public static bool GetProp(this JsonObject obj, string propname, ref short value) => GetPropCore(obj, propname, ref value);
    public static bool GetProp(this JsonObject obj, string propname, ref ushort value) => GetPropCore(obj, propname, ref value);
    public static bool GetProp(this JsonObject obj, string propname, ref int value) => GetPropCore(obj, propname, ref value);
    public static bool GetProp(this JsonObject obj, string propname, ref uint value) => GetPropCore(obj, propname, ref value);
    public static bool GetProp(this JsonObject obj, string propname, ref long value) => GetPropCore(obj, propname, ref value);
    public static bool GetProp(this JsonObject obj, string propname, ref ulong value) => GetPropCore(obj, propname, ref value);
    public static bool GetProp(this JsonObject obj, string propname, ref float value) => GetPropCore(obj, propname, ref value);
    public static bool GetProp(this JsonObject obj, string propname, ref double value) => GetPropCore(obj, propname, ref value);
    public static bool GetProp(this JsonObject obj, string propname, ref bool value) => GetPropCore(obj, propname, ref value);
    public static bool GetProp(this JsonObject obj, string propname, [MaybeNullWhen(false)] ref string value) => GetPropCore(obj, propname, ref value);
    public static bool GetProp<T>(this JsonObject obj, string propname, ref T value) where T : IFromJson<T>
    {
        var prop = obj[propname];
        if(prop == null) { return false; }
        value = T.FromJson(prop);
        return true;
    }

    public static bool GetEnumProp<T>(this JsonObject obj, string propname, ref T value) where T : struct, Enum
    {
        var prop = obj[propname];
        if(prop == null) { return false; }
        value = EnumJsonHelper.ToEnum<T>(prop);
        return true;
    }

    public static void GetPropOrThrow(this JsonObject obj, string propname, out sbyte value) => GetPropOrThrowCore(obj, propname, out value);
    public static void GetPropOrThrow(this JsonObject obj, string propname, out byte value) => GetPropOrThrowCore(obj, propname, out value);
    public static void GetPropOrThrow(this JsonObject obj, string propname, out short value) => GetPropOrThrowCore(obj, propname, out value);
    public static void GetPropOrThrow(this JsonObject obj, string propname, out ushort value) => GetPropOrThrowCore(obj, propname, out value);
    public static void GetPropOrThrow(this JsonObject obj, string propname, out int value) => GetPropOrThrowCore(obj, propname, out value);
    public static void GetPropOrThrow(this JsonObject obj, string propname, out uint value) => GetPropOrThrowCore(obj, propname, out value);
    public static void GetPropOrThrow(this JsonObject obj, string propname, out long value) => GetPropOrThrowCore(obj, propname, out value);
    public static void GetPropOrThrow(this JsonObject obj, string propname, out ulong value) => GetPropOrThrowCore(obj, propname, out value);
    public static void GetPropOrThrow(this JsonObject obj, string propname, out float value) => GetPropOrThrowCore(obj, propname, out value);
    public static void GetPropOrThrow(this JsonObject obj, string propname, out double value) => GetPropOrThrowCore(obj, propname, out value);
    public static void GetPropOrThrow(this JsonObject obj, string propname, out bool value) => GetPropOrThrowCore(obj, propname, out value);
    public static void GetPropOrThrow(this JsonObject obj, string propname, out string value) => GetPropOrThrowCore(obj, propname, out value);
    public static void GetPropOrThrow<T>(this JsonObject obj, string propname, out T value) where T : IFromJson<T>
    {
        var prop = obj[propname];
        if(prop == null) {
            ThrowPropertyNotFound(propname);
        }
        value = T.FromJson(prop);
    }
    public static bool GetEnumPropOrThrow<T>(this JsonObject obj, string propname, out T value) where T : struct, Enum
    {
        var prop = obj[propname];
        if(prop == null) {
            ThrowPropertyNotFound(propname);
        }
        value = EnumJsonHelper.ToEnum<T>(prop);
        return true;
    }

    private static bool GetPropCore<T>(JsonObject obj, string propname, [MaybeNullWhen(false)] ref T value)
    {
        var prop = obj[propname];
        if(prop == null) { return false; }
        value = prop.AsValue().GetValue<T>();
        return true;
    }

    private static void GetPropOrThrowCore<T>(this JsonObject obj, string propname, out T value)
    {
        var prop = obj[propname];
        if(prop == null) {
            ThrowPropertyNotFound(propname);
        }
        value = prop.AsValue().GetValue<T>();
    }

    [DoesNotReturn]
    private static void ThrowPropertyNotFound(string propname)
    {
        throw new FormatException($"property \"{propname}\" is not found");
    }
}
