#nullable enable
using Elffy.Effective;
using Elffy.Imaging;
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
    private string _text;
    private EventSource<Button> _clicked;

    public string Text
    {
        get => _text;
        set
        {
            if(value == _text) { return; }
            _text = value;
            RequestUpdateMaterial();
        }
    }

    static Button()
    {
        Serializer.RegisterConstructor(FromJson);
        UILayer.RegisterShader<Button>(static layer =>
        {
            return DefaultUIShader.Create(layer).Cast<UIShader>();
        });
    }

    public static Button FromJson(JsonElement element) => new Button(element);

    protected override JsonNode ToJsonProtected()
    {
        var node = base.ToJsonProtected();
        node["text"] = _text;
        return node;
    }

    public Button() : base()
    {
        _text = "";
    }

    private Button(JsonElement element) : base(element)
    {
        _text = "";
        if(element.TryGetProperty("text", out var text)) {
            _text = Serializer.Instantiate<string>(text);
        }
    }
}

public sealed class Panel : UIElement, IFromJson<Panel>
{
    static Panel()
    {
        Serializer.RegisterConstructor(FromJson);
        UILayer.RegisterShader<Panel>(static layer =>
        {
            return DefaultUIShader.Create(layer).Cast<UIShader>();
        });
    }

    public static Panel FromJson(JsonElement element) => new Panel(element);

    public Panel()
    {
    }

    private Panel(JsonElement element) : base(element)
    {
    }

    protected override JsonNode ToJsonProtected()
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

    protected virtual JsonNode ToJsonProtected()
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

    protected void RequestUpdateLayout()
    {
        _needToUpdate |= NeedToUpdateFlags.Layout;
    }

    protected void RequestUpdateMaterial()
    {
        _needToUpdate |= NeedToUpdateFlags.Material;
    }

    internal void SetParent(UIElement parent)
    {
        Debug.Assert(_parent == null);
        _parent = parent;
    }

    internal UIModel CreateModel(UILayer layer)
    {
        Debug.Assert(_model == null);
        var shader = layer.GetRegisteredShader(GetType());
        var model = new UIModel(this, shader);
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
        var actualBorderRadius = UILayouter.DecideBorderRadius(_borderRadius, rect.Size);
        var modelMatrix =
            new Vector3(rect.Position, 0f).ToTranslationMatrix4() *
            //Rotation.ToMatrix4() *
            new Matrix4(
                new Vector4(rect.Size.X, 0, 0, 0),
                new Vector4(0, rect.Size.Y, 0, 0),
                new Vector4(0, 0, 1, 0),
                new Vector4(0, 0, 0, 1));

        var result = new UIUpdateResult
        {
            ActualRect = rect,
            ActualBorderWidth = _borderWidth.ToVector4(),
            ActualBorderRadius = actualBorderRadius,
            MvpMatrix = uiProjection * modelMatrix,
            BackgroundColor = _backgroundColor,
            BorderColor = _borderColor,
        };
        model.Material.UpdateMaterial(this, result);
    }

    protected virtual RectF DecideRect(in ContentAreaInfo parentContentArea)
    {
        return UILayouter.DecideRect(this, parentContentArea);
    }
}

public readonly ref struct UIUpdateResult
{
    public required RectF ActualRect { get; init; }
    public required Vector4 ActualBorderWidth { get; init; }
    public required Vector4 ActualBorderRadius { get; init; }
    public required Matrix4 MvpMatrix { get; init; }
    public required Brush BackgroundColor { get; init; }
    public required Brush BorderColor { get; init; }
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
    private readonly Own<BindGroupLayout> _bindGroupLayout0;
    private readonly Own<BindGroupLayout> _bindGroupLayout1;
    private readonly List<UIElement> _rootElements = new List<UIElement>();
    private bool _isLayoutDirty = false;

    private readonly Dictionary<Type, Own<UIShader>> _shaderCache = new();
    private readonly object _shaderCacheLock = new();
    private bool _isShaderCacheAvailable = true;

    private static readonly ConcurrentDictionary<Type, Func<UILayer, Own<UIShader>>> _shaderFactory = new();

    public static bool RegisterShader<T>(Func<UILayer, Own<UIShader>> valueFactory) where T : UIElement
    {
        ArgumentNullException.ThrowIfNull(valueFactory);
        return _shaderFactory.TryAdd(typeof(T), valueFactory);
    }

    internal UIShader GetRegisteredShader(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        Own<UIShader> value;
        lock(_shaderCacheLock) {
            if(_isShaderCacheAvailable == false) {
                throw new InvalidOperationException("already dead");
            }
            if(_shaderCache.TryGetValue(type, out value) == false) {
                if(_shaderFactory.TryGetValue(type, out var factory) == false) {
                    Throw(type);
                }
                value = factory.Invoke(this);
                if(value.IsNone) {
                    Throw(type);
                }
                Dead.Subscribe(_ => value.Dispose()).AddTo(Subscriptions);
                _shaderCache.Add(type, value);
            }
        }
        return value.AsValue();

        [DoesNotReturn] static void Throw(Type type) => throw new ArgumentException($"shader for {type} is not found");
    }

    public BindGroupLayout BindGroupLayout0 => _bindGroupLayout0.AsValue();
    public BindGroupLayout BindGroupLayout1 => _bindGroupLayout1.AsValue();

    public UILayer(Screen screen, int sortOrder)
        : base(
            screen,
            CreatePipelineLayout(screen, out var bindGroupLayout0, out var bindGroupLayout1),
            sortOrder)
    {
        _bindGroupLayout0 = bindGroupLayout0;
        _bindGroupLayout1 = bindGroupLayout1;
        screen.Resized.Subscribe(arg =>
        {
            _isLayoutDirty = true;
        }).AddTo(Subscriptions);
        Dead.Subscribe(_ =>
        {
            lock(_shaderCacheLock) {
                _isShaderCacheAvailable = false;
                _shaderCache.Clear();
            }
        }).AddTo(Subscriptions);
    }

    protected override void Release()
    {
        base.Release();
        _bindGroupLayout0.Dispose();
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

    private static Own<PipelineLayout> CreatePipelineLayout(Screen screen, out Own<BindGroupLayout> layout0, out Own<BindGroupLayout> layout1)
    {
        return PipelineLayout.Create(screen, new()
        {
            BindGroupLayouts = new[]
            {
                // group 0
                BindGroupLayout.Create(screen, new()
                {
                    Entries = new[]
                    {
                        BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex | ShaderStages.Fragment, new BufferBindingData { Type = BufferBindingType.Uniform } ),
                        BindGroupLayoutEntry.Buffer(1, ShaderStages.Vertex | ShaderStages.Fragment, new BufferBindingData { Type = BufferBindingType.Uniform } ),
                    },
                }).AsValue(out layout0),

                // group 1
                BindGroupLayout.Create(screen, new()
                {
                    Entries = new[]
                    {
                        BindGroupLayoutEntry.Texture(0, ShaderStages.Vertex | ShaderStages.Fragment, new TextureBindingData
                        {
                            ViewDimension = TextureViewDimension.D2,
                            Multisampled = false,
                            SampleType = TextureSampleType.FloatNotFilterable,
                        }),
                        BindGroupLayoutEntry.Sampler(1, ShaderStages.Vertex | ShaderStages.Fragment, SamplerBindingType.NonFiltering),
                    },
                }).AsValue(out layout1),
            },
        });
    }
}

public abstract class UIShader : Shader<UIShader, UIMaterial, UILayer>
{
    protected UIShader(
        ReadOnlySpan<byte> shaderSource,
        UILayer operation,
        Func<PipelineLayout, ShaderModule, RenderPipelineDescriptor> getPipelineDesc)
        : base(shaderSource, operation, getPipelineDesc)
    {
    }

    public abstract Own<UIMaterial> CreateMaterial();
}

internal sealed class DefaultUIShader : UIShader
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
        @group(1) @binding(0) var tex: texture_2d<f32>;
        @group(1) @binding(1) var tex_sampler: sampler;

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

        fn get_texel_color(fragcoord: vec2<f32>) -> vec4<f32> {
            let tex_size: vec2<i32> = textureDimensions(tex, 0).xy;
            let offset_in_rect: vec2<f32> = data.rect.xy + ((data.rect.zw - vec2<f32>(tex_size).xy) * 0.5);
            let texel_pos: vec2<f32> = fragcoord - offset_in_rect;
            if(texel_pos.x < 0.0 || texel_pos.x >= f32(tex_size.x) || texel_pos.y < 0.0 || texel_pos.y >= f32(tex_size.y)) {
                return vec4<f32>(0.0, 0.0, 0.0, 0.0);
            }
            else {
                return textureLoad(tex, vec2<i32>(texel_pos), 0);
            }
        }

        @fragment fn fs_main(
            f: V2F,
        ) -> @location(0) vec4<f32> {
            // pixel coordinates, which is not normalized
            let fragcoord: vec2<f32> = f.clip_pos.xy;
            let texel_color = get_texel_color(fragcoord);
            var color: vec4<f32> = blend(texel_color, data.solid_color, 1.0);

            let b_radius = data.border_radius;

            // top-left corner
            let center_tl = data.rect.xy + vec2<f32>(b_radius.x, b_radius.x);
            if(fragcoord.x < center_tl.x && fragcoord.y < center_tl.y) {
                let d = fragcoord - center_tl;
                let len_d = length(d);
                if(len_d > b_radius.x + 1.0) {
                    discard;
                }
                if(len_d > b_radius.x) {
                    return vec4<f32>(
                        data.border_solid_color.rgb, 
                        data.border_solid_color.a * (1.0 - (len_d - b_radius.x)),
                    );
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
                    return blend(data.border_solid_color, color, 1.0 - diff);
                }
            }

            // top-right corner
            let center_tr = data.rect.xy + vec2<f32>(data.rect.z - b_radius.y, b_radius.y);
            if(fragcoord.x >= center_tr.x && fragcoord.y < center_tr.y) {
                let d = fragcoord - center_tr;
                let len_d = length(d);
                if(len_d > b_radius.x + 1.0) {
                    discard;
                }
                if(len_d > b_radius.x) {
                    return vec4<f32>(
                        data.border_solid_color.rgb, 
                        data.border_solid_color.a * (1.0 - (len_d - b_radius.x)),
                    );
                }
                var a = b_radius.y - data.border_width.y;   // x-axis radius of ellipse
                var b = b_radius.y - data.border_width.x;   // y-axis radius of ellipse

                // vector from center of ellipse to the crossed point of 'd' and the ellipse
                let v: vec2<f32> = d * a * b / sqrt(pow_x2(b * d.x) + pow_x2(a * d.y));
                let len_v = length(v);
                if(len_d > len_v) {
                    return data.border_solid_color;
                }
                let diff = len_v - len_d;
                if(diff <= 1.0) {
                    return blend(data.border_solid_color, color, 1.0 - diff);
                }
            }

            // bottom-right corner
            let center_br = data.rect.xy + vec2<f32>(data.rect.z - b_radius.z, data.rect.w - b_radius.z);
            if(fragcoord.x >= center_br.x && fragcoord.y >= center_br.y) {
                let d = fragcoord - center_br;
                let len_d = length(d);
                if(len_d > b_radius.x + 1.0) {
                    discard;
                }
                if(len_d > b_radius.x) {
                    return vec4<f32>(
                        data.border_solid_color.rgb, 
                        data.border_solid_color.a * (1.0 - (len_d - b_radius.x)),
                    );
                }
                var a = b_radius.z - data.border_width.y;   // x-axis radius of ellipse
                var b = b_radius.z - data.border_width.z;   // y-axis radius of ellipse

                // vector from center of ellipse to the crossed point of 'd' and the ellipse
                let v: vec2<f32> = d * a * b / sqrt(pow_x2(b * d.x) + pow_x2(a * d.y));
                let len_v = length(v);
                if(len_d > len_v) {
                    return data.border_solid_color;
                }
                let diff = len_v - len_d;
                if(diff <= 1.0) {
                    return blend(data.border_solid_color, color, 1.0 - diff);
                }
            }

            // bottom-left corner
            let center_bl = data.rect.xy + vec2<f32>(b_radius.w, data.rect.w - b_radius.w);
            if(fragcoord.x < center_bl.x && fragcoord.y >= center_bl.y) {
                let d = fragcoord - center_bl;
                let len_d = length(d);
                if(len_d > b_radius.x + 1.0) {
                    discard;
                }
                if(len_d > b_radius.x) {
                    return vec4<f32>(
                        data.border_solid_color.rgb, 
                        data.border_solid_color.a * (1.0 - (len_d - b_radius.x)),
                    );
                }
                var a = b_radius.z - data.border_width.w;   // x-axis radius of ellipse
                var b = b_radius.z - data.border_width.z;   // y-axis radius of ellipse

                // vector from center of ellipse to the crossed point of 'd' and the ellipse
                let v: vec2<f32> = d * a * b / sqrt(pow_x2(b * d.x) + pow_x2(a * d.y));
                let len_v = length(v);
                if(len_d > len_v) {
                    return data.border_solid_color;
                }
                let diff = len_v - len_d;
                if(diff <= 1.0) {
                    return blend(data.border_solid_color, color, 1.0 - diff);
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
            return color;
        }
        """u8;

    private readonly Own<Texture> _emptyTexture;
    private readonly Own<Sampler> _emptyTextureSampler;

    private DefaultUIShader(UILayer operation)
        : base(ShaderSource, operation, Desc)
    {
        _emptyTexture = Texture.Create(operation.Screen, new TextureDescriptor
        {
            Dimension = TextureDimension.D2,
            Format = TextureFormat.Rgba8UnormSrgb,
            MipLevelCount = 1,
            SampleCount = 1,
            Size = new Vector3u(1, 1, 1),
            Usage = TextureUsages.TextureBinding,
        });
        _emptyTextureSampler = Sampler.Create(operation.Screen, new SamplerDescriptor
        {
            AddressModeU = AddressMode.ClampToEdge,
            AddressModeV = AddressMode.ClampToEdge,
            AddressModeW = AddressMode.ClampToEdge,
            MagFilter = FilterMode.Nearest,
            MinFilter = FilterMode.Nearest,
            MipmapFilter = FilterMode.Nearest,
        });
    }

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        _emptyTexture.Dispose();
        _emptyTextureSampler.Dispose();
    }

    private static RenderPipelineDescriptor Desc(PipelineLayout layout, ShaderModule module)
    {
        var screen = layout.Screen;
        return new RenderPipelineDescriptor
        {
            Layout = layout,
            Vertex = new VertexState()
            {
                Module = module,
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
                Module = module,
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
        };
    }

    public static Own<DefaultUIShader> Create(UILayer layer)
    {
        return CreateOwn(new DefaultUIShader(layer));
    }

    public override Own<UIMaterial> CreateMaterial()
    {
        return DefaultUIShader.Material.Create(this, _emptyTexture.AsValue(), _emptyTextureSampler.AsValue()).Cast<UIMaterial>();
    }

    private sealed class Material : UIMaterial
    {
        private readonly Own<Buffer> _buffer;
        private readonly Own<BindGroup> _bindGroup0;
        private Own<BindGroup> _bindGroup1;
        private MaybeOwn<Texture> _texture;
        private readonly MaybeOwn<Sampler> _sampler;

        [StructLayout(LayoutKind.Sequential, Pack = WgslConst.AlignOf_mat4x4_f32)]
        private readonly struct BufferData
        {
            public required Matrix4 Mvp { get; init; }
            public required Color4 SolidColor { get; init; }
            public required RectF Rect { get; init; }
            public required Vector4 BorderWidth { get; init; }
            public required Vector4 BorderRadius { get; init; }
            public required Color4 BorderSolidColor { get; init; }
        }

        public override BindGroup BindGroup0 => _bindGroup0.AsValue();
        public override BindGroup BindGroup1 => _bindGroup1.AsValue();

        private Material(
            UIShader shader,
            Own<BindGroup> bindGroup0,
            Own<BindGroup> bindGroup1,
            Own<Buffer> buffer,
            MaybeOwn<Texture> texture,
            MaybeOwn<Sampler> sampler)
            : base(shader)
        {
            _bindGroup0 = bindGroup0;
            _bindGroup1 = bindGroup1;
            _texture = texture;
            _buffer = buffer;
            _sampler = sampler;
        }

        public override void Validate()
        {
            base.Validate();
            _texture.Validate();
            _sampler.Validate();
        }

        protected override void Release(bool manualRelease)
        {
            base.Release(manualRelease);
            if(manualRelease) {
                _bindGroup0.Dispose();
                _bindGroup1.Dispose();
                _buffer.Dispose();
                _texture.Dispose();
                _sampler.Dispose();
            }
        }

        private void WriteUniform(in BufferData data)
        {
            var buffer = _buffer.AsValue();
            buffer.WriteData(0, data);
        }

        internal static Own<Material> Create(UIShader shader, MaybeOwn<Texture> texture, MaybeOwn<Sampler> sampler)
        {
            var screen = shader.Screen;
            var buffer = Buffer.Create(screen, (nuint)Unsafe.SizeOf<BufferData>(), BufferUsages.Uniform | BufferUsages.CopyDst);
            var bindGroup0 = BindGroup.Create(screen, new()
            {
                Layout = shader.Operation.BindGroupLayout0,
                Entries = new[]
                {
                    BindGroupEntry.Buffer(0, screen.InfoBuffer),
                    BindGroupEntry.Buffer(1, buffer.AsValue()),
                },
            });
            var bindGroup1 = CreateBindGroup1(screen, shader.Operation.BindGroupLayout1, texture.AsValue().View, sampler.AsValue());
            var self = new Material(shader, bindGroup0, bindGroup1, buffer, texture, sampler);
            return CreateOwn(self);
        }

        private static Own<BindGroup> CreateBindGroup1(Screen screen, BindGroupLayout layout, TextureView textureView, Sampler sampler)
        {
            return BindGroup.Create(screen, new()
            {
                Layout = layout,
                Entries = new[]
                {
                    BindGroupEntry.TextureView(0, textureView),
                    BindGroupEntry.Sampler(1, sampler),
                },
            });
        }

        public override void UpdateMaterial(UIElement element, in UIUpdateResult result)
        {
            // TODO:
            if(element is Button button) {
                UpdateForButton(button);
            }

            WriteUniform(new()
            {
                Mvp = result.MvpMatrix,
                SolidColor = result.BackgroundColor.SolidColor,
                Rect = result.ActualRect,
                BorderWidth = result.ActualBorderWidth,
                BorderRadius = result.ActualBorderRadius,
                BorderSolidColor = result.BorderColor.SolidColor,
            });
        }

        private void UpdateForButton(Button button)
        {
            var text = button.Text;
            if(string.IsNullOrEmpty(text) == false) {
                using var font = new SkiaSharp.SKFont();
                font.Size = 12;
                var options = new TextDrawOptions
                {
                    Background = ColorByte.Transparent,
                    Foreground = ColorByte.Black,
                    Font = font,
                };
                TextDrawer.Draw(text, options, this, static (image, x) =>
                {
                    var (self, metrics) = x;
                    var texture = Texture.CreateFromRawData(self.Shader.Screen, new TextureDescriptor
                    {
                        Dimension = TextureDimension.D2,
                        Format = TextureFormat.Rgba8UnormSrgb,
                        MipLevelCount = 1,
                        SampleCount = 1,
                        Size = new Vector3u((uint)image.Width, (uint)image.Height, 1),
                        Usage = TextureUsages.TextureBinding,
                    }, image.GetPixels().AsBytes());

                    var bindGroup1 = CreateBindGroup1(self.Screen, self.Operation.BindGroupLayout1, texture.AsValue().View, self._sampler.AsValue());
                    self._bindGroup1.Dispose();
                    self._texture.Dispose();
                    self._bindGroup1 = bindGroup1;
                    self._texture = texture;
                });
            }
        }
    }
}

internal static class WgslConst
{
    public const int AlignOf_mat4x4_f32 = 16;
}

public abstract class UIMaterial : Material<UIMaterial, UIShader, UILayer>
{
    public abstract BindGroup BindGroup0 { get; }
    public abstract BindGroup BindGroup1 { get; }

    protected UIMaterial(UIShader shader) : base(shader)
    {
    }

    public abstract void UpdateMaterial(UIElement element, in UIUpdateResult result);
}

public sealed class UIModel : FrameObject<UIModel, UILayer, VertexSlim, UIShader, UIMaterial>
{
    private readonly UIElement _element;

    internal UIElement Element => _element;

    internal UIModel(UIElement element, UIShader shader)
        : base(
            GetMesh(shader.Screen),
            shader.CreateMaterial())
    {
        _element = element;
        IsFrozen = true;
    }

    protected override void Render(in RenderPass pass, UIMaterial material, Mesh<VertexSlim> mesh)
    {
        pass.SetVertexBuffer(0, mesh.VertexBuffer);
        pass.SetIndexBuffer(mesh.IndexBuffer, mesh.IndexFormat);
        pass.SetBindGroup(0, material.BindGroup0);
        pass.SetBindGroup(1, material.BindGroup1);
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

internal static class UILayouter
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
