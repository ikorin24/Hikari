#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elffy.UI;

public class Button : UIElement, IFromJson<Button>
{
    private EventSource<Button> _clicked;

    static Button() => Serializer.RegisterConstructor(FromJson);
    public static Button FromJson(JsonNode? node) => new Button(node);

    protected override JsonNode? ToJsonProtected(JsonSerializerOptions? options)
    {
        var node = base.ToJsonProtected(options);
        return node;
    }

    public Button()
    {
    }

    protected Button(JsonNode? node) : base(node)
    {
    }
}

public class Panel : UIElement, IFromJson<Panel>
{
    static Panel() => Serializer.RegisterConstructor(FromJson);
    public static Panel FromJson(JsonNode? node) => new Panel(node);

    public Panel()
    {
    }

    protected Panel(JsonNode? node) : base(node)
    {
    }

    protected override JsonNode? ToJsonProtected(JsonSerializerOptions? options)
    {
        var node = base.ToJsonProtected(options);
        return node;
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

    protected UIElement(JsonNode? node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var obj = node.AsObject();

        obj.MaySetTo("width", ref _width);
        obj.MaySetTo("height", ref _height);

        var children = obj["children"];
        if(children != null) {
            _children = new List<UIElement>(Serializer.Instantiate<UIElement[]>(children));
        }
    }

    protected virtual JsonNode? ToJsonProtected(JsonSerializerOptions? options)
    {
        var obj = new JsonObject()
        {
            ["@type"] = GetType().FullName,
            ["width"] = _width,
            ["height"] = _height,
        };
        var children = _children;
        if(children != null) {
            var array = new JsonArray();
            foreach(var child in children) {
                var childJson = ((IToJson)child).ToJson(options);
                array.Add(childJson);
            }
            obj["children"] = array;
        }
        return obj;
    }

    JsonNode? IToJson.ToJson(JsonSerializerOptions? options)
    {
        var node = ToJsonProtected(options);
        return node;
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
