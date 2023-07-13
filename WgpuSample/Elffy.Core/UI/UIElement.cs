#nullable enable
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elffy.UI;

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
