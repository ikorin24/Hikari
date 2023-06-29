#nullable enable
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elffy.UI;

public class Button : UIElement, IFromJson<Button>
{
    private EventSource<Button> _clicked;

    static Button() => Serializer.Register(FromJson);
    public static Button FromJson(JsonObject obj) => new Button(obj);

    protected override JsonObject ToJsonProtected()
    {
        var obj = base.ToJsonProtected();
        return obj;
    }

    public Button()
    {
    }

    protected Button(JsonObject obj) : base(obj)
    {
    }
}

public class Panel : UIElement, IFromJson<Panel>
{
    static Panel() => Serializer.Register(FromJson);
    public static Panel FromJson(JsonObject obj) => new Panel(obj);

    public Panel()
    {
    }

    protected Panel(JsonObject obj) : base(obj)
    {
    }

    protected override JsonObject ToJsonProtected()
    {
        var obj = base.ToJsonProtected();
        return obj;
    }
}

public abstract class UIElement : IToJson
{
    private readonly List<UIElement>? _children;
    private float _width;
    private float _height;

    public float Width
    {
        get => _width;
        set => _width = value;
    }
    public float Height
    {
        get => _height;
        set => _height = value;
    }

    public UIElement[] Children
    {
        init => (_children ??= new()).AddRange(value);
    }

    protected UIElement()
    {
    }

    protected UIElement(JsonObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        obj.MaySetTo("width", ref _width);
        obj.MaySetTo("height", ref _height);

        var children = obj["children"];
        if(children != null) {
            _children = new List<UIElement>(Serializer.Instantiate<UIElement[]>(children));
        }
    }

    protected virtual JsonObject ToJsonProtected()
    {
        return new JsonObject()
        {
            ["width"] = _width,
            ["height"] = _height,
        };
    }

    JsonObject IToJson.ToJson(JsonSerializerOptions? options)
    {
        var obj = ToJsonProtected();
        obj["@type"] = GetType().FullName;
        return obj;
    }
}

public static class Serializer
{
    private record ConstructorFunc(Type Type, Func<JsonObject, object> Func);

    private static readonly ConcurrentDictionary<string, ConstructorFunc> _constructorFuncs = new();
    private static readonly ConcurrentDictionary<string, Type> _shortNames = new()
    {
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

    public static void Register<T>(Func<JsonObject, T> constructoFunc) where T : notnull
    {
        Func<JsonObject, object> f = arg => constructoFunc(arg);
        var value = new ConstructorFunc(typeof(T), f);
        _constructorFuncs.TryAdd(typeof(T).FullName!, value);
    }

    public static string Serialize<T>(T value) where T : notnull, IToJson
    {
        var obj = value.ToJson(DefaultWriteSerializerOptions);
        return obj.ToJsonString(DefaultWriteSerializerOptions);
    }

    public static void Serialize<T>(T value, IBufferWriter<byte> bufferWriter) where T : notnull, IToJson
    {
        using var writer = new Utf8JsonWriter(bufferWriter, DefaultWriterOptions);
        var obj = value.ToJson(DefaultWriteSerializerOptions);
        obj.WriteTo(writer, DefaultWriteSerializerOptions);
    }

    public static T Deserialize<T>([StringSyntax(StringSyntaxAttribute.Json)] ReadOnlySpan<byte> utf8Json) where T : notnull
    {
        RuntimeHelpers.RunClassConstructor(typeof(T).TypeHandle);
        var node = JsonNode.Parse(utf8Json, documentOptions: ParseOptions) ?? throw new FormatException("null");
        return (T)ParseJsonNode(node, typeof(T));
    }

    public static T Deserialize<T>([StringSyntax(StringSyntaxAttribute.Json)] string json) where T : notnull
    {
        RuntimeHelpers.RunClassConstructor(typeof(T).TypeHandle);
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
        switch(node) {
            case JsonObject obj: {
                return ParseJsonObject(obj, hint);
            }
            case JsonArray array: {
                return ParseJsonArray(array, hint);
            }
            case JsonValue value: {
                return ParseJsonValue(value, hint);
            }
            default:
                throw new UnreachableException();
        }

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

        static bool FindCtorFromType(Type type, [MaybeNullWhen(false)] out ConstructorFunc ctor)
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

        static bool FindCtorFromName(string typeFullName, [MaybeNullWhen(false)] out ConstructorFunc ctor)
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

    private static object ParseJsonArray(JsonArray array, Type hint)
    {
        if(hint.IsSZArray) {
            var elementType = hint.GetElementType() ?? throw new UnreachableException();
            var a = Array.CreateInstance(elementType, array.Count);
            for(int i = 0; i < a.Length; i++) {
                a.SetValue(ParseJsonNode(array[i], elementType), i);
            }
            return a;
        }
        throw new FormatException();
    }

    private static object ParseJsonValue(JsonValue value, Type hint)
    {
        throw new NotImplementedException();
    }
}

public interface IFromJson<TSelf>
{
    abstract static TSelf FromJson(JsonObject obj);
}

public interface IToJson
{
    JsonObject ToJson(JsonSerializerOptions? options = null);
}

internal static class JsonObjectExtensions
{
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

internal sealed class UILayer : ObjectLayer<UILayer, VertexSlim, UIShader, UIMaterial, UIModel>
{
    private readonly List<UIElement> _elements = new List<UIElement>();

    public UILayer(Screen screen, Own<UIShader> shader, Func<UIShader, Own<RenderPipeline>> pipelineGen, int sortOrder) : base(screen, shader, pipelineGen, sortOrder)
    {
    }

    protected override void RenderShadowMap(in RenderShadowMapContext context, ReadOnlySpan<UIModel> objects)
    {
    }
}

internal sealed class UIShader : Shader<UIShader, UIMaterial>
{
    public UIShader(Screen screen, ReadOnlySpan<byte> shaderSource, in PipelineLayoutDescriptor pipelineLayoutDesc) : base(screen, shaderSource, pipelineLayoutDesc)
    {
    }
}

internal sealed class UIMaterial : Material<UIMaterial, UIShader>
{
    public UIMaterial(UIShader shader) : base(shader)
    {
    }
}

internal sealed class UIModel : Renderable<UIModel, UILayer, VertexSlim, UIShader, UIMaterial>
{
    private readonly UIElement _element;

    public UIElement Element => _element;

    public UIModel(UIElement element, UILayer layer, MaybeOwn<Mesh<VertexSlim>> mesh, Own<UIMaterial> material) : base(layer, mesh, material)
    {
        _element = element;
    }

    protected override void Render(in RenderPass pass, UIMaterial material, Mesh<VertexSlim> mesh)
    {
        throw new NotImplementedException();
    }
}

public enum LayoutLengthType
{
    Length,
    Proportion,
}
