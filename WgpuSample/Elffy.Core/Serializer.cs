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
        var node = value.ToJson(DefaultWriteSerializerOptions);
        return node?.ToJsonString(DefaultWriteSerializerOptions) ?? "null";
    }

    public static void Serialize<T>(T value, IBufferWriter<byte> bufferWriter) where T : notnull, IToJson
    {
        using var writer = new Utf8JsonWriter(bufferWriter, DefaultWriterOptions);
        var node = value.ToJson(DefaultWriteSerializerOptions);
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
    private static object? ParseJsonNode(JsonNode? node, Type hint)
    {
        return node switch
        {
            JsonArray array => ParseJsonArray(array, hint),
            JsonObject obj => ParseJsonObject(obj, hint),
            JsonValue value => ParseJsonValue(value, hint),
            null => null,
            _ => throw new NotSupportedException(),
        };
    }

    private static object ParseJsonObject(JsonObject obj, Type hint)
    {
        obj.MustSetTo("@type", out string? typename);
        if(typename == null) {
            throw new FormatException($"type is null");
        }
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
        if(ctor.Type.IsAssignableTo(hint) == false) {
            throw new FormatException($"{ctor.Type.FullName} is not assignable to {hint.FullName}");
        }
        return ctor.Func(obj);
    }

    private static object ParseJsonValue(JsonValue value, Type hint)
    {
        if(FindCtorFromType(hint, out var ctor) == false) {
            throw new FormatException($"type \"{hint.FullName}\" cannot be created from json");
        }
        return ctor.Func(value);
    }

    private static object ParseJsonArray(JsonArray array, Type hint)
    {
        if(FindCtorFromType(hint, out var ctor)) {
            return ctor.Func(array);
        }

        if(hint.IsSZArray) {
            var elementType = hint.GetElementType() ?? throw new UnreachableException();
            var a = Array.CreateInstance(elementType, array.Count);
            for(int i = 0; i < a.Length; i++) {
                a.SetValue(ParseJsonNode(array[i], elementType), i);
            }
            return a;
        }
        throw new FormatException($"type \"{hint.FullName}\" cannot be created from json");
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
    JsonNode? ToJson(JsonSerializerOptions? options = null);
}

internal static class JsonObjectExtensions
{
    // obj.GetPropValue("hoge", ref _hoge);
    public static bool MaySetTo(this JsonObject obj, string propname, ref sbyte value) => SetPrivate(obj, propname, ref value);
    public static bool MaySetTo(this JsonObject obj, string propname, ref byte value) => SetPrivate(obj, propname, ref value);
    public static bool MaySetTo(this JsonObject obj, string propname, ref short value) => SetPrivate(obj, propname, ref value);
    public static bool MaySetTo(this JsonObject obj, string propname, ref ushort value) => SetPrivate(obj, propname, ref value);
    public static bool MaySetTo(this JsonObject obj, string propname, ref int value) => SetPrivate(obj, propname, ref value);
    public static bool MaySetTo(this JsonObject obj, string propname, ref uint value) => SetPrivate(obj, propname, ref value);
    public static bool MaySetTo(this JsonObject obj, string propname, ref long value) => SetPrivate(obj, propname, ref value);
    public static bool MaySetTo(this JsonObject obj, string propname, ref ulong value) => SetPrivate(obj, propname, ref value);
    public static bool MaySetTo(this JsonObject obj, string propname, ref float value) => SetPrivate(obj, propname, ref value);
    public static bool MaySetTo(this JsonObject obj, string propname, ref double value) => SetPrivate(obj, propname, ref value);
    public static bool MaySetTo(this JsonObject obj, string propname, ref bool value) => SetPrivate(obj, propname, ref value);
    public static bool MaySetTo(this JsonObject obj, string propname, [MaybeNullWhen(false)] ref string value) => SetPrivate(obj, propname, ref value);
    public static void MustSetTo(this JsonObject obj, string propname, [MaybeNullWhen(false)] out string value)
    {
        Unsafe.SkipInit(out value);
        if(SetPrivate(obj, propname, ref value) == false) {
            ThrowPropertyNotFound(propname);
        }
    }

    private static void ThrowPropertyNotFound(string propname)
    {
        throw new FormatException($"property \"{propname}\" is not found");
    }

    private static bool SetPrivate<T>(JsonObject obj, string propname, [MaybeNullWhen(false)] ref T value)
    {
        var prop = obj[propname];
        if(prop == null) { return false; }
        value = prop.AsValue().GetValue<T>();
        return true;
    }
}
