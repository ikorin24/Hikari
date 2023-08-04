#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Elffy.UI;

public abstract class UIElement : IToJson, IReactive
{
    private UIModel? _model;
    private UIElement? _parent;
    private readonly UIElementCollection _children;
    private EventSource<UIModel> _modelAlive;
    private EventSource<UIModel> _modelEarlyUpdate;
    private EventSource<UIModel> _modelUpdate;
    private EventSource<UIModel> _modelLateUpdate;
    private EventSource<UIModel> _modelTerminated;
    private EventSource<UIModel> _modelDead;
    private readonly SubscriptionBag _modelSubscriptions = new SubscriptionBag();

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
    private PseudoClassState _pseudoClass;

    private LayoutResult _layoutResult;
    private bool _isMouseOver;
    private bool _isMouseOverPrev;
    private NeedToUpdateFlags _needToUpdate;

    [Flags]
    private enum NeedToUpdateFlags : uint
    {
        None = 0,
        Material = 1,
        Layout = 2,
    }

    private readonly struct LayoutResult
    {
        public required RectF Rect { get; init; }
        public required Vector4 BorderRadius { get; init; }
    }

    internal Event<UIModel> ModelAlive => _modelAlive.Event;
    internal Event<UIModel> ModelEarlyUpdate => _modelEarlyUpdate.Event;
    internal Event<UIModel> ModelUpdate => _modelUpdate.Event;
    internal Event<UIModel> ModelLateUpdate => _modelLateUpdate.Event;
    internal Event<UIModel> ModelTerminated => _modelTerminated.Event;
    internal Event<UIModel> ModelDead => _modelDead.Event;
    internal SubscriptionRegister ModelSubscriptions => _modelSubscriptions.Register;

    public UIElement? Parent => _parent;
    internal UIModel? Model => _model;
    public Screen? Screen => _model?.Screen;

    public void Remove()
    {
        Model?.Terminate();
    }


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

    protected bool IsMouseOver => _isMouseOver;
    protected bool IsMouseOverPrev => _isMouseOverPrev;

    private static LayoutLength DefaultWidth => new LayoutLength(1f, LayoutLengthType.Proportion);
    private static LayoutLength DefaultHeight => new LayoutLength(1f, LayoutLengthType.Proportion);
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

    protected UIElement(in ReactSource source) : this()
    {
        if(source.TryGetProperty(nameof(Width), out var width)) {
            _width = LayoutLength.FromJson(width);
        }
        if(source.TryGetProperty(nameof(Height), out var height)) {
            _height = LayoutLength.FromJson(height);
        }
        if(source.TryGetProperty(nameof(Margin), out var margin)) {
            _margin = Thickness.FromJson(margin);
        }
        if(source.TryGetProperty(nameof(Padding), out var padding)) {
            _padding = Thickness.FromJson(padding);
        }
        if(source.TryGetProperty(nameof(HorizontalAlignment), out var horizontalAlignment)) {
            _horizontalAlignment = horizontalAlignment.ToEnum<HorizontalAlignment>();
        }
        if(source.TryGetProperty(nameof(VerticalAlignment), out var verticalAlignment)) {
            _verticalAlignment = verticalAlignment.ToEnum<VerticalAlignment>();
        }
        if(source.TryGetProperty(nameof(BackgroundColor), out var backgroundColor)) {
            _backgroundColor = Brush.FromJson(backgroundColor);
        }
        if(source.TryGetProperty(nameof(BorderWidth), out var borderWidth)) {
            _borderWidth = Thickness.FromJson(borderWidth);
        }
        if(source.TryGetProperty(nameof(BorderRadius), out var borderRadius)) {
            _borderRadius = CornerRadius.FromJson(borderRadius);
        }
        if(source.TryGetProperty(nameof(BorderColor), out var borderColor)) {
            _borderColor = Brush.FromJson(borderColor);
        }
        foreach(var (name, value) in source.EnumerateProperties()) {
            if(name.StartsWith("&:")) {
                var pseudo = name.AsSpan(2);
                switch(pseudo) {
                    case nameof(PseudoClass.Hover): {
                        _pseudoClass.Set(PseudoClass.Hover, value);
                        break;
                    }
                    default: {
                        break;
                    }
                }
            }
        }

        if(source.TryGetProperty(nameof(Children), out var children)) {
            Children = UIElementCollection.FromJson(children);
        }
    }

    protected virtual void ToJsonProtected(Utf8JsonWriter writer)
    {
        writer.WriteString("@type", GetType().FullName);
        writer.Write(nameof(Width), _width);
        writer.Write(nameof(Height), _height);
        writer.Write(nameof(Margin), _margin);
        writer.Write(nameof(Padding), _padding);
        writer.WriteEnum(nameof(HorizontalAlignment), _horizontalAlignment);
        writer.WriteEnum(nameof(VerticalAlignment), _verticalAlignment);
        writer.Write(nameof(BackgroundColor), _backgroundColor);
        writer.Write(nameof(BorderWidth), _borderWidth);
        writer.Write(nameof(BorderRadius), _borderRadius);
        writer.Write(nameof(BorderColor), _borderColor);
        writer.Write(nameof(Children), _children);
    }

    void IReactive.ApplyDiff(in ReactSource source)
    {
        ApplyDiffProtected(source);
    }

    protected virtual void ApplyDiffProtected(in ReactSource source)
    {
        Width = source.ApplyProperty(nameof(Width), Width, () => DefaultWidth, out _);
        Height = source.ApplyProperty(nameof(Height), Height, () => DefaultHeight, out _);
        Margin = source.ApplyProperty(nameof(Margin), Margin, () => DefaultMargin, out _);
        Padding = source.ApplyProperty(nameof(Padding), Padding, () => DefaultPadding, out _);
        HorizontalAlignment = source.ApplyProperty(nameof(HorizontalAlignment), HorizontalAlignment, () => DefaultHorizontalAlignment, out _);
        VerticalAlignment = source.ApplyProperty(nameof(VerticalAlignment), VerticalAlignment, () => DefaultVerticalAlignment, out _);
        BackgroundColor = source.ApplyProperty(nameof(BackgroundColor), BackgroundColor, () => DefaultBackgroundColor, out _);
        BorderWidth = source.ApplyProperty(nameof(BorderWidth), BorderWidth, () => DefaultBorderWidth, out _);
        BorderRadius = source.ApplyProperty(nameof(BorderRadius), BorderRadius, () => DefaultBorderRadius, out _);
        BorderColor = source.ApplyProperty(nameof(BorderColor), BorderColor, () => DefaultBorderColor, out _);

        if(source.TryGetProperty(nameof(Children), out var childrenProp)) {
            childrenProp.ApplyDiff(Children);
        }
    }

    public JsonValueKind ToJson(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        ToJsonProtected(writer);
        writer.WriteEndObject();
        return JsonValueKind.Object;
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

    internal void ClearParent()
    {
        _parent = null;
    }

    internal void CreateModel(UILayer layer)
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
        model.Terminated
            .Subscribe(static model => model.Element._modelTerminated.Invoke(model))
            .AddTo(model.Subscriptions);
        model.Dead
            .Subscribe(static model =>
            {
                model.Element._modelDead.Invoke(model);
                model.Element._modelSubscriptions.Dispose();
            })
            .AddTo(model.Subscriptions);

        model.Terminated.Subscribe(static model =>
        {
            var element = model.Element;
            foreach(var child in element.Children) {
                child.Remove();
            }
        }).AddTo(model.Subscriptions);

        _model = model;
        foreach(var child in _children) {
            child.CreateModel(layer);
        }
    }

    internal void UpdateLayout(bool parentLayoutChanged, in ContentAreaInfo parentContentArea, Mouse mouse)
    {
        bool layoutChanged;
        //mouse.Position

        // 'rect' is top-left based in Screen
        // When the top-left corner of the UIElement whose size is (200, 100) is placed at (10, 40) in screen,
        // 'rect' is { X = 10, Y = 40, Width = 200, Heigh = 100 }
        RectF rect;
        Vector4 borderRadius;
        if(parentLayoutChanged || _needToUpdate.HasFlag(NeedToUpdateFlags.Layout)) {
            rect = DecideRect(parentContentArea);
            borderRadius = UILayouter.DecideBorderRadius(_borderRadius, rect.Size);
            _needToUpdate &= ~NeedToUpdateFlags.Layout;
            _needToUpdate |= NeedToUpdateFlags.Material;
            layoutChanged = true;
        }
        else {
            rect = _layoutResult.Rect;
            borderRadius = _layoutResult.BorderRadius;
            layoutChanged = false;
        }

        var isMouseOver = HitTest(rect, mouse.Position, borderRadius);

        if(isMouseOver != _isMouseOver) {
            _needToUpdate |= NeedToUpdateFlags.Material;
        }
        _isMouseOverPrev = _isMouseOver;
        _isMouseOver = isMouseOver;
        _layoutResult = new LayoutResult
        {
            Rect = rect,
            BorderRadius = borderRadius,
        };
        var contentArea = new ContentAreaInfo
        {
            Rect = rect,
            Padding = _padding,
        };
        foreach(var child in _children) {
            child.UpdateLayout(layoutChanged, contentArea, mouse);
        }
    }

    private static bool HitTest(in RectF rect, in Vector2 mousePos, in Vector4 borderRadius)
    {
        var isMouseOver = rect.Contains(mousePos)
            && BorderRadiusTL(rect, mousePos, borderRadius.X)
            && BorderRadiusTR(rect, mousePos, borderRadius.Y)
            && BorderRadiusBR(rect, mousePos, borderRadius.Z)
            && BorderRadiusBL(rect, mousePos, borderRadius.W);
        return isMouseOver;

        static bool BorderRadiusTL(in RectF rect, in Vector2 mousePos, float r)
        {
            var center = rect.Position + new Vector2(r, r);
            if(new RectF(rect.X, rect.Y, r, r).Contains(mousePos) && (center - mousePos).Length > r) {
                return false;
            }
            return true;
        }

        static bool BorderRadiusTR(in RectF rect, in Vector2 mousePos, float r)
        {
            var center = rect.Position + new Vector2(rect.Width, 0) + new Vector2(-r, r);
            if(new RectF(rect.X + rect.Width - r, rect.Y, r, r).Contains(mousePos) && (center - mousePos).Length > r) {
                return false;
            }
            return true;
        }

        static bool BorderRadiusBR(in RectF rect, in Vector2 mousePos, float r)
        {
            var center = rect.Position + rect.Size - new Vector2(r, r);
            if(new RectF(center, new(r, r)).Contains(mousePos) && (center - mousePos).Length > r) {
                return false;
            }
            return true;
        }

        static bool BorderRadiusBL(in RectF rect, in Vector2 mousePos, float r)
        {
            var center = rect.Position + new Vector2(0, rect.Height) + new Vector2(r, -r);
            if(new RectF(rect.X, rect.Y + rect.Height - r, r, r).Contains(mousePos) && (center - mousePos).Length > r) {
                return false;
            }
            return true;
        }
    }

    internal void UpdateMaterial(in Vector2u screenSize, in Matrix4 uiProjection)
    {
        var model = _model;
        Debug.Assert(model != null);

        if(model != null && _needToUpdate.HasFlag(NeedToUpdateFlags.Material)) {
            var result = _layoutResult;

            // origin is bottom-left of rect because clip space is bottom-left based
            var modelOrigin = new Vector3
            {
                X = result.Rect.Position.X,
                Y = screenSize.Y - result.Rect.Position.Y - result.Rect.Size.Y,
                Z = 0,
            };
            var modelMatrix =
                modelOrigin.ToTranslationMatrix4() *
                new Matrix4(
                    new Vector4(result.Rect.Size.X, 0, 0, 0),
                    new Vector4(0, result.Rect.Size.Y, 0, 0),
                    new Vector4(0, 0, 1, 0),
                    new Vector4(0, 0, 0, 1));

            var backgroundColor = _backgroundColor;
            var borderWidth = _borderWidth;
            var borderColor = _borderColor;
            if(_isMouseOver && _pseudoClass.TryGet(PseudoClass.Hover, out var source)) {
                // properties for material
                // BackgroundColor, BorderWidth, BorderColor
                if(source.TryGetProperty(nameof(BackgroundColor), out var backgroundColorProp)) {
                    backgroundColor = backgroundColorProp.Instantiate<Brush>();
                }
                if(source.TryGetProperty(nameof(BorderWidth), out var borderWidthProp)) {
                    borderWidth = borderWidthProp.Instantiate<Thickness>();
                }
                if(source.TryGetProperty(nameof(BorderColor), out var borderColorProp)) {
                    borderColor = borderColorProp.Instantiate<Brush>();
                }
            }

            var a = new UIUpdateResult
            {
                ActualRect = result.Rect,
                ActualBorderWidth = borderWidth.ToVector4(),
                ActualBorderRadius = result.BorderRadius,
                MvpMatrix = uiProjection * modelMatrix,
                BackgroundColor = backgroundColor,
                BorderColor = borderColor,
                IsMouseOver = _isMouseOver,
                IsMouseOverPrev = _isMouseOverPrev,
            };
            model.Material.UpdateMaterial(this, a);
            _needToUpdate &= ~NeedToUpdateFlags.Material;
        }
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
    public required bool IsMouseOver { get; init; }
    public required bool IsMouseOverPrev { get; init; }
}

internal enum PseudoClass
{
    Hover = 0,
}

internal struct PseudoClassState
{
    private ReactSource _hover;

    public PseudoClassState()
    {
    }

    public void Set(PseudoClass pseudoClass, ReactSource source)
    {
        GetRef(pseudoClass) = source;
    }

    public bool TryGet(PseudoClass pseudoClass, out ReactSource source)
    {
        source = GetRef(pseudoClass);
        return source.IsEmpty == false;
    }

    [UnscopedRef]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref ReactSource GetRef(PseudoClass pc)
    {
        switch(pc) {
            case PseudoClass.Hover:
                return ref _hover;
            default:
                throw new ArgumentException(pc.ToString());
        }
    }
}
