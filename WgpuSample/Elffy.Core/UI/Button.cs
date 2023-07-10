#nullable enable
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;

namespace Elffy.UI;

public sealed class Button : UIElement, IFromJson<Button>
{
    private EventSource<Button> _clicked;

    static Button() => Serializer.RegisterConstructor(FromJson);
    public static Button FromJson(JsonElement element) => new Button(element);

    protected override JsonNode? ToJsonProtected()
    {
        var node = base.ToJsonProtected();
        return node;
    }

    public Button() : base()
    {
    }

    private Button(JsonElement element) : base(element)
    {
    }
}

public sealed class Panel : UIElement, IFromJson<Panel>
{
    static Panel() => Serializer.RegisterConstructor(FromJson);
    public static Panel FromJson(JsonElement element) => new Panel(element);

    public Panel()
    {
    }

    private Panel(JsonElement element) : base(element)
    {
    }

    protected override JsonNode? ToJsonProtected()
    {
        var node = base.ToJsonProtected();
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
    private Thickness _margin;
    private Thickness _padding;
    private HorizontalAlignment _horizontalAlignment;
    private VerticalAlignment _verticalAlignment;
    private Brush _backgroundColor;
    private Thickness _borderWidth;
    private CornerRadius _borderRadius;
    private Brush _borderColor;

    private RectF _actualRect;
    private NeedToUpdateFlags _needToUpdate;

    [Flags]
    private enum NeedToUpdateFlags : uint
    {
        None = 0,
        Material = 1,
        Layout = 2,
    }

    public UIElement? Parent => _parent;
    internal UIModel? Model => _model;
    public Screen? Screen => _model?.Screen;


    public LayoutLength Width
    {
        get => _width;
        set
        {
            if(value == _width) { return; }
            _width = value;
            _needToUpdate |= NeedToUpdateFlags.Layout | NeedToUpdateFlags.Material;
        }
    }
    public LayoutLength Height
    {
        get => _height;
        set
        {
            if(value == _height) { return; }
            _height = value;
            _needToUpdate |= NeedToUpdateFlags.Layout | NeedToUpdateFlags.Material;
        }
    }

    public Thickness Margin
    {
        get => _margin;
        set
        {
            if(value == _margin) { return; }
            _margin = value;
            _needToUpdate |= NeedToUpdateFlags.Layout | NeedToUpdateFlags.Material;
        }
    }

    public Thickness Padding
    {
        get => _padding;
        set
        {
            if(value == _padding) { return; }
            _padding = value;
            _needToUpdate |= NeedToUpdateFlags.Layout | NeedToUpdateFlags.Material;
        }
    }

    public HorizontalAlignment HorizontalAlignment
    {
        get => _horizontalAlignment;
        set
        {
            if(value == _horizontalAlignment) { return; }
            _horizontalAlignment = value;
            _needToUpdate |= NeedToUpdateFlags.Layout | NeedToUpdateFlags.Material;
        }
    }

    public VerticalAlignment VerticalAlignment
    {
        get => _verticalAlignment;
        set
        {
            if(value == _verticalAlignment) { return; }
            _verticalAlignment = value;
            _needToUpdate |= NeedToUpdateFlags.Layout | NeedToUpdateFlags.Material;
        }
    }

    public Brush BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if(value == _backgroundColor) { return; }
            _backgroundColor = value;
            _needToUpdate |= NeedToUpdateFlags.Material;
        }
    }

    public Thickness BorderWidth
    {
        get => _borderWidth;
        set
        {
            if(value == _borderWidth) { return; }
            _borderWidth = value;
            _needToUpdate |= NeedToUpdateFlags.Layout | NeedToUpdateFlags.Material;
        }
    }

    public CornerRadius BorderRadius
    {
        get => _borderRadius;
        set
        {
            if(value == _borderRadius) { return; }
            _borderRadius = value;
            _needToUpdate |= NeedToUpdateFlags.Layout | NeedToUpdateFlags.Material;
        }
    }

    public Brush BorderColor
    {
        get => _borderColor;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _borderColor = value;
            _needToUpdate |= NeedToUpdateFlags.Material;
        }
    }

    public UIElementCollection Children
    {
        get => _children;
        init
        {
            _children = value;
            value.Parent = this;
        }
    }

    protected UIElement()
    {
        // set default values
        _width = new LayoutLength(100f, LayoutLengthType.Length);
        _height = new LayoutLength(100f, LayoutLengthType.Length);
        _margin = new Thickness(0f);
        _padding = new Thickness(0f);
        _horizontalAlignment = HorizontalAlignment.Center;
        _verticalAlignment = VerticalAlignment.Center;
        _backgroundColor = Brush.White;
        _borderWidth = new Thickness(0f);
        _borderRadius = CornerRadius.Zero;
        _borderColor = Brush.Black;
        _children = new UIElementCollection();
        _needToUpdate = NeedToUpdateFlags.Material | NeedToUpdateFlags.Layout;
    }

    protected UIElement(JsonElement element) : this()
    {
        if(element.TryGetProperty("width", out var width)) {
            _width = LayoutLength.FromJson(width);
        }
        if(element.TryGetProperty("height", out var height)) {
            _height = LayoutLength.FromJson(height);
        }
        if(element.TryGetProperty("margin", out var margin)) {
            _margin = Thickness.FromJson(margin);
        }
        if(element.TryGetProperty("padding", out var padding)) {
            _padding = Thickness.FromJson(padding);
        }
        if(element.TryGetProperty("horizontalAlignment", out var horizontalAlignment)) {
            _horizontalAlignment = Enum.Parse<HorizontalAlignment>(horizontalAlignment.GetStringNotNull());
        }
        if(element.TryGetProperty("verticalAlignment", out var verticalAlignment)) {
            _verticalAlignment = Enum.Parse<VerticalAlignment>(verticalAlignment.GetStringNotNull());
        }
        if(element.TryGetProperty("backgroundColor", out var backgroundColor)) {
            _backgroundColor = Brush.FromJson(backgroundColor);
        }
        if(element.TryGetProperty("borderWidth", out var borderWidth)) {
            _borderWidth = Thickness.FromJson(borderWidth);
        }
        if(element.TryGetProperty("borderRadius", out var borderRadius)) {
            _borderRadius = CornerRadius.FromJson(borderRadius);
        }
        if(element.TryGetProperty("borderColor", out var borderColor)) {
            _borderColor = Brush.FromJson(borderColor);
        }
        if(element.TryGetProperty("children", out var children)) {
            _children = UIElementCollection.FromJson(children);
        }
    }

    protected virtual JsonNode? ToJsonProtected()
    {
        var obj = new JsonObject()
        {
            ["@type"] = GetType().FullName,
            ["width"] = _width.ToJson(),
            ["height"] = _height.ToJson(),
            ["margin"] = _margin.ToJson(),
            ["padding"] = _padding.ToJson(),
            ["horizontalAlignment"] = _horizontalAlignment.ToJson(),
            ["verticalAlignment"] = _verticalAlignment.ToJson(),
            ["backgroundColor"] = _backgroundColor.ToJson(),
            ["borderWidth"] = _borderWidth.ToJson(),
            ["borderRadius"] = _borderRadius.ToJson(),
            ["borderColor"] = _borderColor.ToJson(),
            ["children"] = _children.ToJson(),
        };
        return obj;
    }

    public JsonNode? ToJson()
    {
        var node = ToJsonProtected();
        return node;
    }

    internal void SetParent(UIElement parent)
    {
        Debug.Assert(_parent == null);
        _parent = parent;
    }

    internal UIModel CreateModel(UILayer layer)
    {
        Debug.Assert(_model == null);
        var model = new UIModel(this, layer);
        _model = model;
        foreach(var child in _children) {
            child.CreateModel(layer);
        }
        return model;
    }

    internal void UpdateLayout(bool parentLayoutChanged, in ContentAreaInfo parentContentArea, in Matrix4 uiProjection)
    {
        bool layoutChanged;
        RectF rect;
        if(parentLayoutChanged || _needToUpdate.HasFlag(NeedToUpdateFlags.Layout)) {
            rect = DecideRect(parentContentArea);
            _needToUpdate &= ~NeedToUpdateFlags.Layout;
            layoutChanged = true;
        }
        else {
            rect = _actualRect;
            layoutChanged = false;
        }
        var contentArea = new ContentAreaInfo
        {
            Rect = rect,
            Padding = _padding,
        };
        if(parentLayoutChanged || _needToUpdate.HasFlag(NeedToUpdateFlags.Material)) {
            UpdateMaterial(rect, uiProjection);
            _needToUpdate &= ~NeedToUpdateFlags.Material;
        }
        foreach(var child in _children) {
            child.UpdateLayout(layoutChanged, contentArea, uiProjection);
        }
    }

    private void UpdateMaterial(in RectF rect, in Matrix4 uiProjection)
    {
        _actualRect = rect;
        var model = _model;
        Debug.Assert(model != null);
        if(model == null) { return; }
        var actualBorderRadius = UIDefaultLayouter.DecideBorderRadius(_borderRadius, rect.Size);
        var modelMatrix =
            new Vector3(rect.Position, 0f).ToTranslationMatrix4() *
            //Rotation.ToMatrix4() *
            new Matrix4(
                new Vector4(rect.Size.X, 0, 0, 0),
                new Vector4(0, rect.Size.Y, 0, 0),
                new Vector4(0, 0, 1, 0),
                new Vector4(0, 0, 0, 1));

        var background = _backgroundColor;
        model.Material.WriteUniform(new UIMaterial.BufferData
        {
            Mvp = uiProjection * modelMatrix,
            SolidColor = background.Type switch
            {
                BrushType.Solid => background.SolidColor,
                _ => default,
            },
            Rect = rect,
            BorderWidth = _borderWidth.ToVector4(),
            BorderRadius = actualBorderRadius,
            BorderSolidColor = _borderColor.SolidColor,
        });
    }

    protected virtual RectF DecideRect(in ContentAreaInfo parentContentArea)
    {
        return UIDefaultLayouter.DecideRect(this, parentContentArea);
    }
}

public sealed class UIElementCollection
    : IEnumerable<UIElement>,
      IFromJson<UIElementCollection>,
      IToJson
{
    private UIElement? _parent;
    private readonly List<UIElement> _children;

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

    public UIElement this[int index]
    {
        get => _children[index];
    }

    static UIElementCollection() => Serializer.RegisterConstructor(FromJson);

    public UIElementCollection()
    {
        _children = new List<UIElement>();
    }

    private UIElementCollection(List<UIElement> inner)
    {
        _children = inner;
    }

    internal UIElementCollection(ReadOnlySpan<UIElement> elements)
    {
        _children = new List<UIElement>(elements.Length);
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

    public static UIElementCollection FromJson(JsonElement element)
    {
        var list = new List<UIElement>(element.GetArrayLength());
        foreach(var item in element.EnumerateArray()) {
            var child = Serializer.Instantiate<UIElement>(item);
            list.Add(child);
        }
        return new UIElementCollection(list);
    }

    public JsonNode? ToJson()
    {
        var children = _children;
        var array = new JsonArray();
        foreach(var child in children) {
            array.Add(child.ToJson());
        }
        return array;
    }
}

public sealed class UILayer : ObjectLayer<UILayer, VertexSlim, UIShader, UIMaterial, UIModel>
{
    private readonly List<UIElement> _rootElements = new List<UIElement>();
    private bool _isLayoutDirty = false;

    public UILayer(Screen screen, int sortOrder)
        : base(
            screen,
            UIShader.Create(screen),
            CreatePipeline,
            sortOrder)
    {
        screen.Resized.Subscribe(arg =>
        {
            _isLayoutDirty = true;
        }).AddTo(Subscriptions);
    }

    private void UpdateLayout()
    {
        var screenSize = Screen.ClientSize;
        Matrix4.OrthographicProjection(0, (float)screenSize.X, 0, (float)screenSize.Y, -1f, 1f, out var proj);
        var GLToWebGpu = new Matrix4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(0, 0, 0.5f, 0),
            new Vector4(0, 0, 0.5f, 1));
        var uiProjection = GLToWebGpu * proj;
        var contentArea = new ContentAreaInfo
        {
            Rect = new RectF(Vector2.Zero, screenSize.ToVector2()),
            Padding = Thickness.Zero,
        };
        var isLayoutDirty = _isLayoutDirty;
        _isLayoutDirty = false;
        foreach(var element in _rootElements) {
            element.UpdateLayout(isLayoutDirty, contentArea, uiProjection);
        }
    }

    public void AddRootElement(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if(element.Parent != null) {
            throw new ArgumentException("the element is already in UI tree");
        }
        var model = element.CreateModel(this);
        model.Alive.Subscribe(model =>
        {
            model.Layer._rootElements.Add(model.Element);
        }).AddTo(Subscriptions);
    }

    protected override void RenderShadowMap(in RenderShadowMapContext context, ReadOnlySpan<UIModel> objects)
    {
    }

    protected override void Render(in RenderPass pass, ReadOnlySpan<UIModel> objects, RenderObjectAction render)
    {
        // UI rendering disables depth buffer.
        // Render in the order of UIElement tree so that child elements are rendered in front.

        UpdateLayout();

        foreach(var element in _rootElements) {
            RenderElementRecursively(pass, element, render);
        }

        static void RenderElementRecursively(in RenderPass pass, UIElement element, RenderObjectAction render)
        {
            var model = element.Model;
            Debug.Assert(model != null);
            render(in pass, model);
            foreach(var child in element.Children) {
                RenderElementRecursively(pass, child, render);
            }
        }
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
                        Blend = BlendState.AlphaBlending,
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
            solid_color: vec4<f32>,
            rect: vec4<f32>,            // (x, y, width, height)
            border_width: vec4<f32>,    // (top, right, bottom, left)
            border_radius: vec4<f32>,   // (top-left, top-right, bottom-right, bottom-left)
            border_solid_color: vec4<f32>,
        }

        @group(0) @binding(0) var<uniform> screen: ScreenInfo;
        @group(0) @binding(1) var<uniform> data: BufferData;

        const PI: f32 = 3.141592653589793;
        const INV_PI: f32 = 0.3183098861837907;

        @vertex fn vs_main(
            v: Vin,
        ) -> V2F {
            var o: V2F;
            o.clip_pos = data.mvp * vec4<f32>(v.pos, 1.0);
            o.uv = v.uv;
            return o;
        }

        fn pow_x2(x: f32) -> f32 {
            return x * x;
        }

        fn blend(src: vec4<f32>, dst: vec4<f32>, x: f32) -> vec4<f32> {
            let a = src.a * x;
            return vec4(
                src.rgb * a + (1.0 - a) * dst.rgb,
                a + (1.0 - a) * dst.a,
            );
        }

        @fragment fn fs_main(
            f: V2F,
        ) -> @location(0) vec4<f32> {
            // pixel coordinates, not normalized
            let fragcoord: vec2<f32> = f.clip_pos.xy;

            let b_radius = data.border_radius;

            // top-left corner
            let center_tl = data.rect.xy + vec2<f32>(b_radius.x, b_radius.x);
            if(fragcoord.x < center_tl.x && fragcoord.y < center_tl.y) {
                let d = fragcoord - center_tl;
                let len_d = length(d);
                if(len_d > b_radius.x + 1.0) {
                    discard;
                }
                let opacity: f32 = 1.0 - (len_d - b_radius.x);
                if(len_d > b_radius.x) {
                    return vec4<f32>(data.border_solid_color.rgb, data.border_solid_color.a * opacity);
                }
                var a = b_radius.x - data.border_width.w;   // x-axis radius of ellipse
                var b = b_radius.x - data.border_width.x;   // y-axis radius of ellipse

                // vector from center of ellipse to the crossed point of 'd' and the ellipse
                let v: vec2<f32> = d * a * b / sqrt(pow_x2(b * d.x) + pow_x2(a * d.y));
                let len_v = length(v);
                if(len_d > len_v) {
                    return data.border_solid_color;
                }
                let diff = len_v - len_d;
                if(diff <= 1.0) {
                    return blend(data.border_solid_color, data.solid_color, 1.0 - diff);
                }
            }

            // top-right corner
            let center_tr = data.rect.xy + vec2<f32>(data.rect.z - b_radius.y, b_radius.y);
            if(fragcoord.x >= center_tr.x && fragcoord.y < center_tr.y) {
                let d = fragcoord - center_tr;
                let d2 = d.x * d.x + d.y * d.y;
                if(d2 > b_radius.y * b_radius.y) {
                    discard;
                }
                if(pow_x2(d.x) / pow_x2(max(0.001, b_radius.y - data.border_width.y)) + pow_x2(d.y) / pow_x2(max(0.001, b_radius.y - data.border_width.x)) > 1.0) {
                    return data.border_solid_color;
                }
            }

            // bottom-right corner
            let center_br = data.rect.xy + vec2<f32>(data.rect.z - b_radius.z, data.rect.w - b_radius.z);
            if(fragcoord.x >= center_br.x && fragcoord.y >= center_br.y) {
                let d = fragcoord - center_br;
                let d2 = d.x * d.x + d.y * d.y;
                if(d2 > b_radius.z * b_radius.z) {
                    discard;
                }
                if(pow_x2(d.x) / pow_x2(max(0.001, b_radius.z - data.border_width.y)) + pow_x2(d.y) / pow_x2(max(0.001, b_radius.z - data.border_width.z)) > 1.0) {
                    return data.border_solid_color;
                }
            }

            // bottom-left corner
            let center_bl = data.rect.xy + vec2<f32>(b_radius.w, data.rect.w - b_radius.w);
            if(fragcoord.x < center_bl.x && fragcoord.y >= center_bl.y) {
                let d = fragcoord - center_bl;
                let d2 = d.x * d.x + d.y * d.y;
                if(d2 > b_radius.w * b_radius.w) {
                    discard;
                }
                if(pow_x2(d.x) / pow_x2(max(0.001, b_radius.w - data.border_width.w)) + pow_x2(d.y) / pow_x2(max(0.001, b_radius.w - data.border_width.z)) > 1.0) {
                    return data.border_solid_color;
                }
            }

            // top border
            if(fragcoord.y < data.rect.y + data.border_width.x) {
                return data.border_solid_color;
            }
            // right border
            if(fragcoord.x >= data.rect.x + data.rect.z - data.border_width.y) {
                return data.border_solid_color;
            }
            // left border
            if(fragcoord.x < data.rect.x + data.border_width.w) {
                return data.border_solid_color;
            }
            // bottom border
            if(fragcoord.y >= data.rect.y + data.rect.w - data.border_width.z) {
                return data.border_solid_color;
            }
            return data.solid_color;
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


internal static class WgslConst
{
    public const int AlignOf_mat4x4_f32 = 16;
}

public sealed class UIMaterial : Material<UIMaterial, UIShader>
{
    private readonly Own<Buffer> _buffer;
    private readonly Own<BindGroup> _bindGroup0;

    [StructLayout(LayoutKind.Sequential, Pack = WgslConst.AlignOf_mat4x4_f32)]
    internal readonly struct BufferData
    {
        public required Matrix4 Mvp { get; init; }
        public required Color4 SolidColor { get; init; }
        public required RectF Rect { get; init; }
        public required Vector4 BorderWidth { get; init; }
        public required Vector4 BorderRadius { get; init; }
        public required Color4 BorderSolidColor { get; init; }
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
        var buffer = Buffer.Create(screen, (nuint)Unsafe.SizeOf<BufferData>(), BufferUsages.Uniform | BufferUsages.CopyDst);
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
        IsFrozen = true;
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
                new VertexSlim(0, 1, 0, 0, 0),
                new VertexSlim(0, 0, 0, 0, 1),
                new VertexSlim(1, 0, 0, 1, 1),
                new VertexSlim(1, 1, 0, 1, 0),
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

    public static Vector4 DecideBorderRadius(in CornerRadius desiredBorderRadius, in Vector2 actualSize)
    {
        var ratio0 = actualSize.X / (desiredBorderRadius.TopLeft + desiredBorderRadius.TopRight);
        var ratio1 = actualSize.Y / (desiredBorderRadius.TopRight + desiredBorderRadius.BottomRight);
        var ratio2 = actualSize.X / (desiredBorderRadius.BottomRight + desiredBorderRadius.BottomLeft);
        var ratio3 = actualSize.Y / (desiredBorderRadius.BottomLeft + desiredBorderRadius.TopLeft);
        var ratio = float.Min(1f, float.Min(float.Min(ratio0, ratio1), float.Min(ratio2, ratio3)));
        return desiredBorderRadius.ToVector4() * ratio;
    }
}

public readonly struct ContentAreaInfo : IEquatable<ContentAreaInfo>
{
    public required RectF Rect { get; init; }
    public required Thickness Padding { get; init; }

    public override bool Equals(object? obj) => obj is ContentAreaInfo info && Equals(info);

    public bool Equals(ContentAreaInfo other)
        => Rect.Equals(other.Rect) &&
           Padding.Equals(other.Padding);

    public override int GetHashCode() => HashCode.Combine(Rect, Padding);

    public static bool operator ==(ContentAreaInfo left, ContentAreaInfo right) => left.Equals(right);

    public static bool operator !=(ContentAreaInfo left, ContentAreaInfo right) => !(left == right);
}
