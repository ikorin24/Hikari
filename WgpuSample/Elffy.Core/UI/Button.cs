#nullable enable
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;

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
    private UIModel? _model;
    private UIElement? _parent;
    private readonly UIElementCollection _children;

    private LayoutLength _width;
    private LayoutLength _height;
    private LayoutThickness _margin;
    private LayoutThickness _padding;
    private HorizontalAlignment _horizontalAlignment;
    private VerticalAlignment _verticalAlignment;

    private Vector2 _actualPosition;
    private Vector2 _actualSize;

    public UIElement? Parent => _parent;
    internal UIModel? Model => _model;
    public Screen? Screen => _model?.Screen;

    public Vector2 ActualPosition
    {
        get => _actualPosition;
        internal set => _actualPosition = value;
    }

    public Vector2 ActualSize
    {
        get => _actualSize;
        internal set => _actualSize = value;
    }


    public LayoutLength Width
    {
        get => _width;
        set => _width = value;
    }
    public LayoutLength Height
    {
        get => _height;
        set => _height = value;
    }

    public LayoutThickness Margin
    {
        get => _margin;
        set => _margin = value;
    }

    public LayoutThickness Padding
    {
        get => _padding;
        set => _padding = value;
    }

    public HorizontalAlignment HorizontalAlignment
    {
        get => _horizontalAlignment;
        set => _horizontalAlignment = value;
    }

    public VerticalAlignment VerticalAlignment
    {
        get => _verticalAlignment;
        set => _verticalAlignment = value;
    }

    public UIElementCollection Children
    {
        init
        {
            _children = value;
            value.Parent = this;
        }
        get => _children;
    }

    protected UIElement()
    {
        _children = new UIElementCollection();
    }

    internal void SetParent(UIElement parent)
    {
        Debug.Assert(_parent == null);
        _parent = parent;
    }

    internal void CreateModel(UILayer layer)
    {
        Debug.Assert(_model == null);
        _model = new UIModel(this, layer);
        foreach(var child in _children) {
            child.CreateModel(layer);
        }
    }

    protected UIElement(JsonNode? node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var obj = node.AsObject();

        //obj.MaySetTo("width", ref _width);
        //obj.MaySetTo("height", ref _height);

        var children = obj["children"];
        if(children != null) {
            _children = new UIElementCollection(Serializer.Instantiate<UIElement[]>(children));
        }
        else {
            _children = new UIElementCollection();
        }
    }

    protected virtual JsonNode? ToJsonProtected(JsonSerializerOptions? options)
    {
        var obj = new JsonObject()
        {
            ["@type"] = GetType().FullName,
            //["width"] = _width,
            //["height"] = _height,
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

    internal void Layout()
    {
        var model = _model;
        var actualRect = UIElementLayouter.Default.LayoutSelf(this);
        _actualPosition = actualRect.Position;
        _actualSize = actualRect.Size;

        //_actualSize = new Vector2(100, 100);  // TODO:

        if(model != null) {
            var screenSize = model.Screen.ClientSize.ToVector2();
            var s0 = _actualSize / screenSize;
            var p0 = _actualPosition / screenSize;

            var s = s0 * 2f;
            var p = new Vector2
            {
                X = p0.X * 2f - 1f,
                Y = (1f - p0.Y - s0.Y) * 2f - 1f,
            };

            var mat = new Matrix4(
                new(s.X, 0, 0, 0),
                new(0, s.Y, 0, 0),
                new(0, 0, 1, 0),
                new(p.X, p.Y, 0, 1));
            model.Material.WriteUniform(new()
            {
                Mvp = mat,
            });
        }
        foreach(var child in _children) {
            child.Layout();
        }
    }

    internal void LayoutChildren()
    {
        var screen = Screen;
        if(screen == null) {
            throw new InvalidOperationException();  // TODO:
        }
        var actualPosition = _actualPosition;

        foreach(var child in _children) {
            var contentArea = MesureContentArea(child);
            var rect = DecideRect(child, contentArea);
            child._actualPosition = rect.Position + actualPosition;
            child._actualSize = rect.Size;
            child.LayoutChildren();
        }
    }

    protected virtual ContentAreaInfo MesureContentArea(UIElement child)
    {
        return new ContentAreaInfo
        {
            Rect = new RectF(Vector2.Zero, _actualSize),
            Padding = _padding,
        };
    }

    protected virtual RectF DecideRect(UIElement child, in ContentAreaInfo area)
    {
        return UIDefaultLayouter.DecideRect(child, in area);
    }
}

public sealed class UIElementCollection : IEnumerable<UIElement>
{
    private UIElement? _parent;
    private readonly List<UIElement> _children = new List<UIElement>();

    internal UIElement? Parent
    {
        get => _parent;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if(Interlocked.CompareExchange(ref _parent, value, null) != null) {
                ThrowInvalidInstance();
            }
            var layer = value.Model?.Layer;
            foreach(var child in _children) {
                child.SetParent(value);
                if(layer != null) {
                    child.CreateModel(layer);
                }
            }
        }
    }

    public UIElementCollection()
    {
    }

    internal UIElementCollection(ReadOnlySpan<UIElement> elements)
    {
        foreach(var element in elements) {
            ArgumentNullException.ThrowIfNull(element);
            _children.Add(element);
        }
    }

    public void Add(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        _children.Add(element);
        var parent = _parent;
        if(parent != null) {
            element.SetParent(parent);
            var layer = parent.Model?.Layer;
            if(layer != null) {
                element.CreateModel(layer);
            }
        }
    }

    [DoesNotReturn]
    private static void ThrowInvalidInstance() => throw new InvalidOperationException("invalid instance");

    public IEnumerator<UIElement> GetEnumerator()
    {
        // TODO: 
        return _children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _children.GetEnumerator();
    }
}

public sealed class UILayer : ObjectLayer<UILayer, VertexSlim, UIShader, UIMaterial, UIModel>
{
    private readonly List<UIElement> _rootElements = new List<UIElement>();

    public UILayer(Screen screen, int sortOrder)
        : base(
            screen,
            UIShader.Create(screen),
            CreatePipeline,
            sortOrder)
    {
    }

    internal void Layout()
    {
        var screenSize = Screen.ClientSize.ToVector2();
        var contentArea = new ContentAreaInfo
        {
            Rect = new RectF(Vector2.Zero, screenSize),
            Padding = LayoutThickness.Zero,
        };
        foreach(var root in _rootElements) {
            var rect = UIDefaultLayouter.DecideRect(root, contentArea);
            root.ActualPosition = rect.Position;
            root.ActualSize = rect.Size;

            // TODO: root.Material.WriteUniform(new() { Mvp = mat });

            root.LayoutChildren();
        }
    }

    public void AddRootElement(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if(element.Parent != null) {
            throw new ArgumentException();
        }
        _rootElements.Add(element);
        element.CreateModel(this);
        element.Layout();
        //Layout();
    }

    protected override void RenderShadowMap(in RenderShadowMapContext context, ReadOnlySpan<UIModel> objects)
    {
    }

    protected override OwnRenderPass CreateRenderPass(in OperationContext context)
    {
        return context.CreateSurfaceRenderPass(colorClear: null, depthStencil: null);
    }

    private static Own<RenderPipeline> CreatePipeline(UIShader shader)
    {
        var screen = shader.Screen;
        return RenderPipeline.Create(screen, new()
        {
            Layout = shader.PipelineLayout,
            Vertex = new VertexState()
            {
                Module = shader.Module,
                EntryPoint = "vs_main"u8.ToArray(),
                Buffers = new VertexBufferLayout[]
                {
                    VertexBufferLayout.FromVertex<VertexSlim>(stackalloc[]
                    {
                        (0, VertexFieldSemantics.Position),
                        (1, VertexFieldSemantics.UV),
                    }),
                },
            },
            Fragment = new FragmentState()
            {
                Module = shader.Module,
                EntryPoint = "fs_main"u8.ToArray(),
                Targets = new ColorTargetState?[]
                {
                    new ColorTargetState
                    {
                        Format = screen.SurfaceFormat,
                        Blend = null,
                        WriteMask = ColorWrites.All,
                    },
                },
            },
            Primitive = new PrimitiveState()
            {
                Topology = PrimitiveTopology.TriangleList,
                FrontFace = FrontFace.Ccw,
                CullMode = Face.Back,
                PolygonMode = PolygonMode.Fill,
                StripIndexFormat = null,
            },
            DepthStencil = null,
            Multisample = MultisampleState.Default,
            Multiview = 0,
        });
    }
}

public sealed class UIShader : Shader<UIShader, UIMaterial>
{
    private static ReadOnlySpan<byte> ShaderSource => """
        struct Vin {
            @location(0) pos: vec3<f32>,
            @location(1) uv: vec2<f32>,
        }
        struct V2F {
            @builtin(position) clip_pos: vec4<f32>,
            @location(0) uv: vec2<f32>,
        }
        struct ScreenInfo {
            size: vec2<u32>,
        }
        struct BufferData
        {
            mvp: mat4x4<f32>,
        }

        @group(0) @binding(0) var<uniform> screen: ScreenInfo;
        @group(0) @binding(1) var<uniform> data: BufferData;

        @vertex fn vs_main(
            v: Vin,
        ) -> V2F {
            var o: V2F;
            o.clip_pos = data.mvp * vec4<f32>(v.pos, 1.0);
            o.uv = v.uv;
            return o;
        }

        @fragment fn fs_main(
            f: V2F,
        ) -> @location(0) vec4<f32> {
            return vec4<f32>(f.uv.x, f.uv.y, 0.0, 1.0);
        }
        """u8;

    private readonly Own<BindGroupLayout> _layout0;

    internal BindGroupLayout Layout0 => _layout0.AsValue();

    private UIShader(Screen screen) :
        base(screen, ShaderSource, Desc(screen, out var layout0))
    {
        _layout0 = layout0;
    }

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        if(manualRelease) {
            _layout0.Dispose();
        }
    }

    internal static Own<UIShader> Create(Screen screen)
    {
        var self = new UIShader(screen);
        return CreateOwn(self);
    }

    private static PipelineLayoutDescriptor Desc(Screen screen, out Own<BindGroupLayout> layout0)
    {
        return new()
        {
            BindGroupLayouts = new[]
            {
                BindGroupLayout.Create(screen, new()
                {
                    Entries = new[]
                    {
                        BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex | ShaderStages.Fragment, new BufferBindingData { Type = BufferBindingType.Uniform } ),
                        BindGroupLayoutEntry.Buffer(1, ShaderStages.Vertex | ShaderStages.Fragment, new BufferBindingData { Type = BufferBindingType.Uniform } ),
                    }
                }).AsValue(out layout0),
            },
        };
    }
}

public sealed class UIMaterial : Material<UIMaterial, UIShader>
{
    private readonly Own<Buffer> _buffer;
    private readonly Own<BindGroup> _bindGroup0;

    internal struct BufferData
    {
        public Matrix4 Mvp;
    }

    internal BindGroup BindGroup0 => _bindGroup0.AsValue();

    private UIMaterial(UIShader shader, Own<BindGroup> bindGroup0, Own<Buffer> buffer) : base(shader)
    {
        _bindGroup0 = bindGroup0;
        _buffer = buffer;
    }

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        if(manualRelease) {
            _bindGroup0.Dispose();
            _buffer.Dispose();
        }
    }

    internal void WriteUniform(in BufferData data)
    {
        var buffer = _buffer.AsValue();
        buffer.WriteData(0, data);
    }

    internal static Own<UIMaterial> Create(UIShader shader)
    {
        var screen = shader.Screen;
        var buffer = Buffer.CreateInitData(screen, new BufferData
        {
            Mvp = Matrix4.Identity,
        }, BufferUsages.Uniform | BufferUsages.CopyDst);
        var bindGroup0 = BindGroup.Create(screen, new()
        {
            Layout = shader.Layout0,
            Entries = new[]
            {
                BindGroupEntry.Buffer(0, screen.InfoBuffer),
                BindGroupEntry.Buffer(1, buffer.AsValue()),
            },
        });
        var self = new UIMaterial(shader, bindGroup0, buffer);
        return CreateOwn(self);
    }
}

public sealed class UIModel : Renderable<UIModel, UILayer, VertexSlim, UIShader, UIMaterial>
{
    private readonly UIElement _element;

    internal UIElement Element => _element;

    internal UIModel(UIElement element, UILayer layer)
        : base(
            layer,
            GetMesh(layer.Screen),
            UIMaterial.Create(layer.Shader))
    {
        _element = element;
    }

    protected override void Render(in RenderPass pass, UIMaterial material, Mesh<VertexSlim> mesh)
    {
        pass.SetVertexBuffer(0, mesh.VertexBuffer);
        pass.SetIndexBuffer(mesh.IndexBuffer, mesh.IndexFormat);
        pass.SetBindGroup(0, material.BindGroup0);
        pass.DrawIndexed(mesh.IndexCount);
    }

    private static Mesh<VertexSlim> GetMesh(Screen screen)
    {
        // TODO: remove cache when screen is disposed

        var cache = _cache.GetOrAdd(screen, static screen =>
        {
            ReadOnlySpan<VertexSlim> vertices = stackalloc VertexSlim[4]
            {
                new VertexSlim(0, 1, 0, 0, 1),
                new VertexSlim(0, 0, 0, 0, 0),
                new VertexSlim(1, 0, 0, 1, 0),
                new VertexSlim(1, 1, 0, 1, 1),
            };
            ReadOnlySpan<ushort> indices = stackalloc ushort[6] { 0, 1, 2, 2, 3, 0 };
            var mesh = Elffy.Mesh.Create(screen, vertices, indices);
            return new MeshCache(mesh);
        });
        return cache.Mesh;
    }

    private static readonly ConcurrentDictionary<Screen, MeshCache> _cache = new();

    private sealed class MeshCache
    {
        private readonly Own<Mesh<VertexSlim>> _mesh;

        public Mesh<VertexSlim> Mesh => _mesh.AsValue();

        public MeshCache(Own<Mesh<VertexSlim>> mesh)
        {
            _mesh = mesh;
        }
    }
}







// --------------------

internal static class UIDefaultLayouter
{
    public static RectF DecideRect(UIElement target, in ContentAreaInfo area)
    {
        var areaSize = Vector2.Max(Vector2.Zero, area.Rect.Size);

        var margin = target.Margin;
        var width = target.Width;
        var height = target.Height;
        var horizontalAlignment = target.HorizontalAlignment;
        var verticalAlignment = target.VerticalAlignment;

        var blankSize = new Vector2
        {
            X = area.Padding.Left + area.Padding.Right + margin.Left + margin.Right,
            Y = area.Padding.Top + area.Padding.Bottom + margin.Top + margin.Bottom,
        };
        var fullSize = Vector2.Max(Vector2.Zero, areaSize - blankSize);

        var sizeCoeff = new Vector2
        {
            X = width.Type switch
            {
                LayoutLengthType.Proportion => areaSize.X,
                LayoutLengthType.Length or _ => 1f,
            },
            Y = height.Type switch
            {
                LayoutLengthType.Proportion => areaSize.Y,
                LayoutLengthType.Length or _ => 1f,
            },
        };
        var size = new Vector2
        {
            X = float.Clamp(width.Value * sizeCoeff.X, 0, fullSize.X),
            Y = float.Clamp(height.Value * sizeCoeff.Y, 0, fullSize.Y),
        };

        var pos = new Vector2
        {
            X = horizontalAlignment switch
            {
                HorizontalAlignment.Left => area.Padding.Left + margin.Left,
                HorizontalAlignment.Right => areaSize.X - area.Padding.Right - margin.Right - size.X,
                HorizontalAlignment.Center or _ => area.Padding.Left + margin.Left + (fullSize.X - size.X) / 2f,
            },
            Y = verticalAlignment switch
            {
                VerticalAlignment.Top => area.Padding.Top + margin.Top,
                VerticalAlignment.Bottom => areaSize.Y - area.Padding.Bottom - margin.Bottom - size.Y,
                VerticalAlignment.Center or _ => area.Padding.Top + margin.Top + (fullSize.Y - size.Y) / 2f,
            },
        } + area.Rect.Position;
        return new RectF(pos, size);
    }
}

public class UIElementLayouter
{
    private static readonly UIElementLayouter _default = new UIElementLayouter();

    public static UIElementLayouter Default => _default;

    internal RectF LayoutSelf(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        var screen = element.Screen;
        if(screen == null) {
            throw new InvalidOperationException();  // TODO:
        }
        var (contentArea, offset) = element.Parent switch
        {
            UIElement parent =>
            (
                ContentArea: MesureContentArea(parent, element),
                Offset: parent.ActualPosition
            ),
            null =>
            (
                ContentArea: new ContentAreaInfo
                {
                    Rect = new RectF(Vector2.Zero, screen.ClientSize.ToVector2()),
                    Padding = LayoutThickness.Zero,
                },
                Offset: Vector2.Zero
            ),
        };
        var rectInParent = DecideRect(element, contentArea);
        var absRect = new RectF(rectInParent.Position + offset, rectInParent.Size);
        return absRect;
    }

    protected virtual ContentAreaInfo MesureContentArea(UIElement parent, UIElement child)
    {
        return new ContentAreaInfo
        {
            Rect = new RectF(Vector2.Zero, parent.ActualSize),
            Padding = parent.Padding,
        };
    }

    protected virtual RectF DecideRect(UIElement target, in ContentAreaInfo area)
    {
        var areaSize = Vector2.Max(Vector2.Zero, area.Rect.Size);

        var margin = target.Margin;
        var width = target.Width;
        var height = target.Height;
        var horizontalAlignment = target.HorizontalAlignment;
        var verticalAlignment = target.VerticalAlignment;

        var blankSize = new Vector2
        {
            X = area.Padding.Left + area.Padding.Right + margin.Left + margin.Right,
            Y = area.Padding.Top + area.Padding.Bottom + margin.Top + margin.Bottom,
        };
        var fullSize = Vector2.Max(Vector2.Zero, areaSize - blankSize);

        var sizeCoeff = new Vector2
        {
            X = width.Type switch
            {
                LayoutLengthType.Proportion => areaSize.X,
                LayoutLengthType.Length or _ => 1f,
            },
            Y = height.Type switch
            {
                LayoutLengthType.Proportion => areaSize.Y,
                LayoutLengthType.Length or _ => 1f,
            },
        };
        var size = new Vector2
        {
            X = float.Clamp(width.Value * sizeCoeff.X, 0, fullSize.X),
            Y = float.Clamp(height.Value * sizeCoeff.Y, 0, fullSize.Y),
        };

        var pos = new Vector2
        {
            X = horizontalAlignment switch
            {
                HorizontalAlignment.Left => area.Padding.Left + margin.Left,
                HorizontalAlignment.Right => areaSize.X - area.Padding.Right - margin.Right - size.X,
                HorizontalAlignment.Center or _ => area.Padding.Left + margin.Left + (fullSize.X - size.X) / 2f,
            },
            Y = verticalAlignment switch
            {
                VerticalAlignment.Top => area.Padding.Top + margin.Top,
                VerticalAlignment.Bottom => areaSize.Y - area.Padding.Bottom - margin.Bottom - size.Y,
                VerticalAlignment.Center or _ => area.Padding.Top + margin.Top + (fullSize.Y - size.Y) / 2f,
            },
        } + area.Rect.Position;
        return new RectF(pos, size);
    }
}

public readonly struct ContentAreaInfo : IEquatable<ContentAreaInfo>
{
    public required RectF Rect { get; init; }
    public required LayoutThickness Padding { get; init; }

    public override bool Equals(object? obj) => obj is ContentAreaInfo info && Equals(info);

    public bool Equals(ContentAreaInfo other)
        => Rect.Equals(other.Rect) &&
           Padding.Equals(other.Padding);

    public override int GetHashCode() => HashCode.Combine(Rect, Padding);

    public static bool operator ==(ContentAreaInfo left, ContentAreaInfo right) => left.Equals(right);

    public static bool operator !=(ContentAreaInfo left, ContentAreaInfo right) => !(left == right);
}
