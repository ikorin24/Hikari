#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elffy.UI;

public abstract class UIElement : IToJson, IReactive
{
    private UIModel? _model;
    private UIElement? _parent;
    private readonly UIElementCollection _children;
    private EventSource<UIModel> _modelEarlyUpdate;
    private EventSource<UIModel> _modelUpdate;
    private EventSource<UIModel> _modelLateUpdate;
    private EventSource<UIModel> _modelAlive;

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

    internal Event<UIModel> ModelAlive => _modelAlive.Event;
    internal Event<UIModel> ModelEarlyUpdate => _modelEarlyUpdate.Event;
    internal Event<UIModel> ModelUpdate => _modelUpdate.Event;
    internal Event<UIModel> ModelLateUpdate => _modelLateUpdate.Event;

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
        [MemberNotNull(nameof(_children))]
        init
        {
            _children = value;
            value.Parent = this;
        }
    }

    private static LayoutLength DefaultWidth => new LayoutLength(100f, LayoutLengthType.Length);
    private static LayoutLength DefaultHeight => new LayoutLength(100f, LayoutLengthType.Length);
    private static Thickness DefaultMargin => new Thickness(0f);
    private static Thickness DefaultPadding => new Thickness(0f);
    private static HorizontalAlignment DefaultHorizontalAlignment => HorizontalAlignment.Center;
    private static VerticalAlignment DefaultVerticalAlignment => VerticalAlignment.Center;
    private static Brush DefaultBackgroundColor => Brush.White;
    private static Thickness DefaultBorderWidth => new Thickness(0f);
    private static CornerRadius DefaultBorderRadius => CornerRadius.Zero;
    private static Brush DefaultBorderColor => Brush.Black;

    protected UIElement()
    {
        // set default values
        _width = DefaultWidth;
        _height = DefaultHeight;
        _margin = DefaultMargin;
        _padding = DefaultPadding;
        _horizontalAlignment = DefaultHorizontalAlignment;
        _verticalAlignment = DefaultVerticalAlignment;
        _backgroundColor = DefaultBackgroundColor;
        _borderWidth = DefaultBorderWidth;
        _borderRadius = DefaultBorderRadius;
        _borderColor = DefaultBorderColor;
        Children = new UIElementCollection();
        _needToUpdate = NeedToUpdateFlags.Material | NeedToUpdateFlags.Layout;
    }

    protected UIElement(JsonElement element, in DeserializeRuntimeData data) : this()
    {
        if(element.TryGetProperty("width", out var width)) {
            _width = LayoutLength.FromJson(width, data);
        }
        if(element.TryGetProperty("height", out var height)) {
            _height = LayoutLength.FromJson(height, data);
        }
        if(element.TryGetProperty("margin", out var margin)) {
            _margin = Thickness.FromJson(margin, data);
        }
        if(element.TryGetProperty("padding", out var padding)) {
            _padding = Thickness.FromJson(padding, data);
        }
        if(element.TryGetProperty("horizontalAlignment", out var horizontalAlignment)) {
            _horizontalAlignment = Enum.Parse<HorizontalAlignment>(horizontalAlignment.GetStringNotNull());
        }
        if(element.TryGetProperty("verticalAlignment", out var verticalAlignment)) {
            _verticalAlignment = Enum.Parse<VerticalAlignment>(verticalAlignment.GetStringNotNull());
        }
        if(element.TryGetProperty("backgroundColor", out var backgroundColor)) {
            _backgroundColor = Brush.FromJson(backgroundColor, data);
        }
        if(element.TryGetProperty("borderWidth", out var borderWidth)) {
            _borderWidth = Thickness.FromJson(borderWidth, data);
        }
        if(element.TryGetProperty("borderRadius", out var borderRadius)) {
            _borderRadius = CornerRadius.FromJson(borderRadius, data);
        }
        if(element.TryGetProperty("borderColor", out var borderColor)) {
            _borderColor = Brush.FromJson(borderColor, data);
        }
        if(element.TryGetProperty("children", out var children)) {
            Children = UIElementCollection.FromJson(children, data);
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

    void IReactive.ApplyDiff(JsonElement element, in DeserializeRuntimeData data)
    {
        ApplyDiffProtected(element, data);
    }

    protected virtual void ApplyDiffProtected(JsonElement element, in DeserializeRuntimeData data)
    {
        Width = element.TryGetProperty("width", out var width)
            ? LayoutLength.FromJson(width, data)
            : DefaultWidth;
        Height = element.TryGetProperty("height", out var height)
            ? LayoutLength.FromJson(height, data)
            : DefaultHeight;
        Margin = element.TryGetProperty("margin", out var margin)
            ? Thickness.FromJson(margin, data)
            : DefaultMargin;
        Padding = element.TryGetProperty("padding", out var padding)
            ? Thickness.FromJson(padding, data)
            : DefaultPadding;
        HorizontalAlignment = element.TryGetProperty("horizontalAlignment", out var horizontalAlignment)
            ? Enum.Parse<HorizontalAlignment>(horizontalAlignment.GetStringNotNull())
            : DefaultHorizontalAlignment;
        VerticalAlignment = element.TryGetProperty("verticalAlignment", out var verticalAlignment)
            ? Enum.Parse<VerticalAlignment>(verticalAlignment.GetStringNotNull())
            : DefaultVerticalAlignment;
        BackgroundColor = element.TryGetProperty("backgroundColor", out var backgroundColor)
            ? Brush.FromJson(backgroundColor, data)
            : DefaultBackgroundColor;
        BorderWidth = element.TryGetProperty("borderWidth", out var borderWidth)
            ? Thickness.FromJson(borderWidth, data)
            : DefaultBorderWidth;
        BorderRadius = element.TryGetProperty("borderRadius", out var borderRadius)
            ? CornerRadius.FromJson(borderRadius, data)
            : DefaultBorderRadius;
        BorderColor = element.TryGetProperty("borderColor", out var borderColor)
            ? Brush.FromJson(borderColor, data)
            : DefaultBorderColor;

        // TODO: children
        throw new NotImplementedException();
        if(element.TryGetProperty("children", out var children)) {
            foreach(var child in children.EnumerateArray()) {
                var key = child.GetProperty("@key"u8).GetStringNotNull();
            }
        }
        else {

        }
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
        model.Alive
            .Subscribe(static model => model.Element._modelAlive.Invoke(model))
            .AddTo(model.Subscriptions);
        model.EarlyUpdate
            .Subscribe(static model => model.Element._modelEarlyUpdate.Invoke(model))
            .AddTo(model.Subscriptions);
        model.Update
            .Subscribe(static model => model.Element._modelUpdate.Invoke(model))
            .AddTo(model.Subscriptions);
        model.LateUpdate
            .Subscribe(static model => model.Element._modelLateUpdate.Invoke(model))
            .AddTo(model.Subscriptions);
        model.Dead
            .Subscribe(static model =>
            {
                var self = model.Element;
                self._modelAlive.Clear();
                self._modelEarlyUpdate.Clear();
                self._modelUpdate.Clear();
                self._modelLateUpdate.Clear();
            })
            .AddTo(model.Subscriptions);
        _model = model;
        foreach(var child in _children) {
            child.CreateModel(layer);
        }
        return model;
    }

    internal void UpdateLayout(bool parentLayoutChanged, in ContentAreaInfo parentContentArea, in Matrix4 uiProjection)
    {
        bool layoutChanged;

        // 'rect' is top-left based in Screen
        // When the top-left corner of the UIElement whose size is (200, 100) is placed at (10, 40) in screen,
        // 'rect' is { X = 10, Y = 40, Width = 200, Heigh = 100 }
        RectF rect;
        if(parentLayoutChanged || _needToUpdate.HasFlag(NeedToUpdateFlags.Layout)) {
            rect = DecideRect(parentContentArea);
            _needToUpdate &= ~NeedToUpdateFlags.Layout;
            _actualRect = rect;
            layoutChanged = true;
        }
        else {
            rect = _actualRect;
            layoutChanged = false;
        }
        if(parentLayoutChanged || _needToUpdate.HasFlag(NeedToUpdateFlags.Material)) {
            UpdateMaterial(rect, uiProjection);
            _needToUpdate &= ~NeedToUpdateFlags.Material;
        }
        var contentArea = new ContentAreaInfo
        {
            Rect = rect,
            Padding = _padding,
        };
        foreach(var child in _children) {
            child.UpdateLayout(layoutChanged, contentArea, uiProjection);
        }
    }

    private void UpdateMaterial(in RectF rect, in Matrix4 uiProjection)
    {
        var model = _model;
        Debug.Assert(model != null);
        if(model == null) { return; }
        var actualBorderRadius = UILayouter.DecideBorderRadius(_borderRadius, rect.Size);

        // origin is bottom-left of rect because clip space is bottom-left based
        var modelOrigin = new Vector3
        {
            X = rect.Position.X,
            Y = model.Screen.ClientSize.Y - rect.Position.Y - rect.Size.Y,
            Z = 0,
        };
        var modelMatrix =
            modelOrigin.ToTranslationMatrix4() *
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

public readonly ref struct UIUpdateResult
{
    public required RectF ActualRect { get; init; }
    public required Vector4 ActualBorderWidth { get; init; }
    public required Vector4 ActualBorderRadius { get; init; }
    public required Matrix4 MvpMatrix { get; init; }
    public required Brush BackgroundColor { get; init; }
    public required Brush BorderColor { get; init; }
}
