#nullable enable
using Elffy.Effective;
using System;
using System.Collections.Immutable;
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
    private EventSource<UIElement> _clicked;
    private readonly SubscriptionBag _modelSubscriptions = new SubscriptionBag();

    private UIElementInfo _info;
    private PseudoClasses _pseudoClasses;

    private (LayoutResult Layout, UIElementInfo AppliedInfo)? _layoutCache;
    private bool _isHover;
    private bool _isClickHolding;
    private bool _needToInvokeClicked;
    private bool _needToLayoutUpdate;

    internal Event<UIModel> ModelAlive => _modelAlive.Event;
    internal Event<UIModel> ModelEarlyUpdate => _modelEarlyUpdate.Event;
    internal Event<UIModel> ModelUpdate => _modelUpdate.Event;
    internal Event<UIModel> ModelLateUpdate => _modelLateUpdate.Event;
    internal Event<UIModel> ModelTerminated => _modelTerminated.Event;
    internal Event<UIModel> ModelDead => _modelDead.Event;
    internal SubscriptionRegister ModelSubscriptions => _modelSubscriptions.Register;

    public Event<UIElement> Clicked => _clicked.Event;
    public UIElement? Parent => _parent;
    internal UIModel? Model => _model;
    public Screen? Screen => _model?.Screen;

    public void Remove()
    {
        Model?.Terminate();
    }


    public LayoutLength Width
    {
        get => _info.Width;
        set
        {
            if(value == _info.Width) { return; }
            _info.Width = value;
            _needToLayoutUpdate = true;
        }
    }
    public LayoutLength Height
    {
        get => _info.Height;
        set
        {
            if(value == _info.Height) { return; }
            _info.Height = value;
            _needToLayoutUpdate = true;
        }
    }

    public Thickness Margin
    {
        get => _info.Margin;
        set
        {
            if(value == _info.Margin) { return; }
            _info.Margin = value;
            _needToLayoutUpdate = true;
        }
    }

    public Thickness Padding
    {
        get => _info.Padding;
        set
        {
            if(value == _info.Padding) { return; }
            _info.Padding = value;
            _needToLayoutUpdate = true;
        }
    }

    public HorizontalAlignment HorizontalAlignment
    {
        get => _info.HorizontalAlignment;
        set
        {
            if(value == _info.HorizontalAlignment) { return; }
            _info.HorizontalAlignment = value;
            _needToLayoutUpdate = true;
        }
    }

    public VerticalAlignment VerticalAlignment
    {
        get => _info.VerticalAlignment;
        set
        {
            if(value == _info.VerticalAlignment) { return; }
            _info.VerticalAlignment = value;
            _needToLayoutUpdate = true;
        }
    }

    public Brush BackgroundColor
    {
        get => _info.BackgroundColor;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if(value == _info.BackgroundColor) { return; }
            _info.BackgroundColor = value;
        }
    }

    public Thickness BorderWidth
    {
        get => _info.BorderWidth;
        set
        {
            if(value == _info.BorderWidth) { return; }
            _info.BorderWidth = value;
            _needToLayoutUpdate = true;
        }
    }

    public CornerRadius BorderRadius
    {
        get => _info.BorderRadius;
        set
        {
            if(value == _info.BorderRadius) { return; }
            _info.BorderRadius = value;
            _needToLayoutUpdate = true;
        }
    }

    public Brush BorderColor
    {
        get => _info.BorderColor;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _info.BorderColor = value;
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
        _info = UIElementInfo.Default;
        _pseudoClasses = new PseudoClasses();
        Children = new UIElementCollection();
        _needToLayoutUpdate = true;

        ModelUpdate.Subscribe(static model =>
        {
            var self = model.Element;
            if(self._needToInvokeClicked) {
                self._needToInvokeClicked = false;
                self._clicked.Invoke(self);
            }
        });
    }

    protected UIElement(in ReactSource source) : this()
    {
        if(source.TryGetProperty(nameof(Width), out var width)) {
            _info.Width = LayoutLength.FromJson(width);
        }
        if(source.TryGetProperty(nameof(Height), out var height)) {
            _info.Height = LayoutLength.FromJson(height);
        }
        if(source.TryGetProperty(nameof(Margin), out var margin)) {
            _info.Margin = Thickness.FromJson(margin);
        }
        if(source.TryGetProperty(nameof(Padding), out var padding)) {
            _info.Padding = Thickness.FromJson(padding);
        }
        if(source.TryGetProperty(nameof(HorizontalAlignment), out var horizontalAlignment)) {
            _info.HorizontalAlignment = horizontalAlignment.ToEnum<HorizontalAlignment>();
        }
        if(source.TryGetProperty(nameof(VerticalAlignment), out var verticalAlignment)) {
            _info.VerticalAlignment = verticalAlignment.ToEnum<VerticalAlignment>();
        }
        if(source.TryGetProperty(nameof(BackgroundColor), out var backgroundColor)) {
            _info.BackgroundColor = Brush.FromJson(backgroundColor);
        }
        if(source.TryGetProperty(nameof(BorderWidth), out var borderWidth)) {
            _info.BorderWidth = Thickness.FromJson(borderWidth);
        }
        if(source.TryGetProperty(nameof(BorderRadius), out var borderRadius)) {
            _info.BorderRadius = CornerRadius.FromJson(borderRadius);
        }
        if(source.TryGetProperty(nameof(BorderColor), out var borderColor)) {
            _info.BorderColor = Brush.FromJson(borderColor);
        }
        if(source.TryGetProperty(nameof(Clicked), out var clicked)) {
            var action = clicked.Instantiate<Action<UIElement>>();
            _clicked.Event.Subscribe(action);
        }


        foreach(var (name, value) in source.EnumerateProperties()) {
            if(name.StartsWith("&:")) {
                var pseudo = name.AsSpan(2);
                switch(pseudo) {
                    case nameof(PseudoClass.Hover): {
                        _pseudoClasses.Set(PseudoClass.Hover, value);
                        break;
                    }
                    case nameof(PseudoClass.Active): {
                        _pseudoClasses.Set(PseudoClass.Active, value);
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
        writer.Write(nameof(Width), _info.Width);
        writer.Write(nameof(Height), _info.Height);
        writer.Write(nameof(Margin), _info.Margin);
        writer.Write(nameof(Padding), _info.Padding);
        writer.WriteEnum(nameof(HorizontalAlignment), _info.HorizontalAlignment);
        writer.WriteEnum(nameof(VerticalAlignment), _info.VerticalAlignment);
        writer.Write(nameof(BackgroundColor), _info.BackgroundColor);
        writer.Write(nameof(BorderWidth), _info.BorderWidth);
        writer.Write(nameof(BorderRadius), _info.BorderRadius);
        writer.Write(nameof(BorderColor), _info.BorderColor);
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

    internal LayoutResult UpdateLayout(bool parentLayoutChanged, in ContentAreaInfo parentContentArea, Mouse mouse)
    {
        // 'rect' is top-left based in Screen
        // When the top-left corner of the UIElement whose size is (200, 100) is placed at (10, 40) in screen,
        // 'rect' is { X = 10, Y = 40, Width = 200, Heigh = 100 }

        var needToRelayout =
            parentLayoutChanged ||
            _needToLayoutUpdate ||
            (mouse.PositionDelta?.IsZero == false && _pseudoClasses[PseudoClass.Hover]?.HasLayoutInfo == true) ||
            mouse.IsChanged(MouseButton.Left);
        var mousePos = mouse.Position;

        var isHoverPrev = _isHover;
        UIElementInfo appliedInfo;
        LayoutResult layout;
        bool isHover;
        if(needToRelayout == false && _layoutCache.HasValue) {
            (layout, appliedInfo) = _layoutCache.Value;
            isHover = _isHover;
        }
        else {
            // First, layout without considering pseudo classes
            appliedInfo = _info;
            var layout1 = Relayout(appliedInfo, parentContentArea);

            if(mousePos.HasValue) {
                var isHover1 = HitTest(mousePos.Value, layout1.Rect, layout1.BorderRadius);

                // layout with considering hover pseudo classes if needed
                if(_pseudoClasses.TryGet(PseudoClass.Hover, out var hoverInfo) && hoverInfo.HasLayoutInfo) {
                    var mergedInfo = appliedInfo.Merged(hoverInfo);
                    var layout2 = Relayout(mergedInfo, parentContentArea);
                    var isHover2 = HitTest(mousePos.Value, layout2.Rect, layout2.BorderRadius);
                    (isHover, layout, appliedInfo) = (isHover1, isHover2) switch
                    {
                        (true, true) => (true, layout2, mergedInfo),
                        (false, false) => (false, layout1, appliedInfo),
                        _ => isHoverPrev ? (true, layout2, mergedInfo) : (false, layout1, appliedInfo),
                    };
                }
                else {
                    (isHover, layout) = (isHover1, layout1);
                }
            }
            else {
                (isHover, layout) = (false, layout1);
            }
        }

        // set or clear flag for click holding
        if(_isClickHolding && mouse.IsUp(MouseButton.Left)) {
            _isClickHolding = false;
            if(isHover) {
                _needToInvokeClicked = true;
            }
        }
        else if(isHover && mouse.IsDown(MouseButton.Left)) {
            _isClickHolding = true;
        }

        // overwrite layout if 'active'
        if(_isClickHolding && _pseudoClasses.TryGet(PseudoClass.Active, out var activeInfo) && activeInfo.HasLayoutInfo) {
            appliedInfo = appliedInfo.Merged(activeInfo);
            layout = Relayout(appliedInfo, parentContentArea);
        }

        _needToLayoutUpdate = false;
        var layoutChanged = _layoutCache?.Layout != layout;
        _isHover = isHover;
        _layoutCache = (layout, appliedInfo);
        var contentArea = new ContentAreaInfo
        {
            Rect = layout.Rect,
            Padding = layout.Padding,
        };
        foreach(var child in _children) {
            child.UpdateLayout(layoutChanged, contentArea, mouse);
        }
        return layout;
    }

    private static LayoutResult Relayout(in UIElementInfo info, in ContentAreaInfo parentContentArea)
    {
        var rect = UILayouter.DecideRect(info, parentContentArea);
        var borderRadius = UILayouter.DecideBorderRadius(info.BorderRadius, rect.Size);
        var layoutResult = new LayoutResult
        {
            Rect = rect,
            BorderRadius = borderRadius,
            Padding = info.Padding,
        };
        return layoutResult;
    }

    private static bool HitTest(in Vector2 mousePos, in RectF rect, in Vector4 borderRadius)
    {
        if(borderRadius.IsZero) {
            return rect.Contains(mousePos);
        }
        else {
            return rect.Contains(mousePos)
                && BorderRadiusTL(rect, mousePos, borderRadius.X)
                && BorderRadiusTR(rect, mousePos, borderRadius.Y)
                && BorderRadiusBR(rect, mousePos, borderRadius.Z)
                && BorderRadiusBL(rect, mousePos, borderRadius.W);
        }

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
        if(model == null) { return; }

        Debug.Assert(_layoutCache != null);
        var ((rect, borderRadius, _), appliedInfo) = _layoutCache.Value;

        // origin is bottom-left of rect because clip space is bottom-left based
        var modelOrigin = new Vector3
        {
            X = rect.Position.X,
            Y = screenSize.Y - rect.Position.Y - rect.Size.Y,
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
            ActualBorderWidth = appliedInfo.BorderWidth.ToVector4(),
            ActualBorderRadius = borderRadius,
            MvpMatrix = uiProjection * modelMatrix,
            BackgroundColor = appliedInfo.BackgroundColor,
            BorderColor = appliedInfo.BorderColor,
            IsHover = _isHover,
        };
        model.Material.UpdateMaterial(this, result);
    }
}

internal record struct UIElementInfo(
    LayoutLength Width,
    LayoutLength Height,
    Thickness Margin,
    Thickness Padding,
    HorizontalAlignment HorizontalAlignment,
    VerticalAlignment VerticalAlignment,
    Brush BackgroundColor,
    Thickness BorderWidth,
    CornerRadius BorderRadius,
    Brush BorderColor
)
{
    internal static UIElementInfo Default => new(
        DefaultWidth,
        DefaultHeight,
        DefaultMargin,
        DefaultPadding,
        DefaultHorizontalAlignment,
        DefaultVerticalAlignment,
        DefaultBackgroundColor,
        DefaultBorderWidth,
        DefaultBorderRadius,
        DefaultBorderColor);

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

    internal readonly UIElementInfo Merged(in UIElementPseudoInfo p)
    {
        return new(
            p.Width ?? Width,
            p.Height ?? Height,
            p.Margin ?? Margin,
            p.Padding ?? Padding,
            p.HorizontalAlignment ?? HorizontalAlignment,
            p.VerticalAlignment ?? VerticalAlignment,
            p.BackgroundColor ?? BackgroundColor,
            p.BorderWidth ?? BorderWidth,
            p.BorderRadius ?? BorderRadius,
            p.BorderColor ?? BorderColor
        );
    }

    internal void Merge(in UIElementPseudoInfo p)
    {
        if(p.Width.HasValue) {
            Width = p.Width.Value;
        }
        if(p.Height.HasValue) {
            Height = p.Height.Value;
        }
        if(p.Margin.HasValue) {
            Margin = p.Margin.Value;
        }
        if(p.Padding.HasValue) {
            Padding = p.Padding.Value;
        }
        if(p.HorizontalAlignment.HasValue) {
            HorizontalAlignment = p.HorizontalAlignment.Value;
        }
        if(p.VerticalAlignment.HasValue) {
            VerticalAlignment = p.VerticalAlignment.Value;
        }
        if(p.BackgroundColor.HasValue) {
            BackgroundColor = p.BackgroundColor.Value;
        }
        if(p.BorderWidth.HasValue) {
            BorderWidth = p.BorderWidth.Value;
        }
        if(p.BorderRadius.HasValue) {
            BorderRadius = p.BorderRadius.Value;
        }
        if(p.BorderColor.HasValue) {
            BorderColor = p.BorderColor.Value;
        }
    }
}

internal readonly record struct UIElementPseudoInfo
    : IFromJson<UIElementPseudoInfo>,
      IToJson
{
    static UIElementPseudoInfo() => Serializer.RegisterConstructor(FromJson);

    private static readonly ImmutableArray<(string PropName, ReactSource Value)> EmptyEx = ImmutableArray<(string PropName, ReactSource Value)>.Empty;

    public LayoutLength? Width { get; init; }
    public LayoutLength? Height { get; init; }
    public Thickness? Margin { get; init; }
    public Thickness? Padding { get; init; }
    public HorizontalAlignment? HorizontalAlignment { get; init; }
    public VerticalAlignment? VerticalAlignment { get; init; }
    public Brush? BackgroundColor { get; init; }
    public Thickness? BorderWidth { get; init; }
    public CornerRadius? BorderRadius { get; init; }
    public Brush? BorderColor { get; init; }

    private readonly ImmutableArray<(string PropName, ReactSource Value)> _ex;
    public ImmutableArray<(string PropName, ReactSource Value)> Ex
    {
        get => _ex.IsDefault ? EmptyEx : _ex;
        init => _ex = value.IsDefault ? EmptyEx : value;
    }

    public bool HasLayoutInfo =>
        Width.HasValue || Height.HasValue || Margin.HasValue ||
        Padding.HasValue || HorizontalAlignment.HasValue || VerticalAlignment.HasValue ||
        BorderWidth.HasValue;

    public static UIElementPseudoInfo FromJson(in ReactSource source)
    {
        LayoutLength? width = null;
        LayoutLength? height = null;
        Thickness? margin = null;
        Thickness? padding = null;
        HorizontalAlignment? horizontalAlignment = null;
        VerticalAlignment? verticalAlignment = null;
        Brush? backgroundColor = null;
        Thickness? borderWidth = null;
        CornerRadius? borderRadius = null;
        Brush? borderColor = null;
        using var ex = new TemporalBuffer<(string PropName, ReactSource Value)>();
        foreach(var (name, value) in source.EnumerateProperties()) {
            switch(name) {
                case nameof(Width): {
                    width = value.Instantiate<LayoutLength>();
                    break;
                }
                case nameof(Height): {
                    height = value.Instantiate<LayoutLength>();
                    break;
                }
                case nameof(Margin): {
                    margin = value.Instantiate<Thickness>();
                    break;
                }
                case nameof(Padding): {
                    padding = value.Instantiate<Thickness>();
                    break;
                }
                case nameof(HorizontalAlignment): {
                    horizontalAlignment = value.Instantiate<HorizontalAlignment>();
                    break;
                }
                case nameof(VerticalAlignment): {
                    verticalAlignment = value.Instantiate<VerticalAlignment>();
                    break;
                }
                case nameof(BackgroundColor): {
                    backgroundColor = value.Instantiate<Brush>();
                    break;
                }
                case nameof(BorderWidth): {
                    borderWidth = value.Instantiate<Thickness>();
                    break;
                }
                case nameof(BorderRadius): {
                    borderRadius = value.Instantiate<CornerRadius>();
                    break;
                }
                case nameof(BorderColor): {
                    borderColor = value.Instantiate<Brush>();
                    break;
                }
                default: {
                    ex.Add((name, value));
                    break;
                }
            }
        }

        return new UIElementPseudoInfo
        {
            Width = width,
            Height = height,
            Margin = margin,
            Padding = padding,
            HorizontalAlignment = horizontalAlignment,
            VerticalAlignment = verticalAlignment,
            BackgroundColor = backgroundColor,
            BorderWidth = borderWidth,
            BorderRadius = borderRadius,
            BorderColor = borderColor,
            Ex = ex.IsEmpty ? EmptyEx : ImmutableArray.Create(ex.AsSpan()),
        };
    }

    public JsonValueKind ToJson(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        if(Width.HasValue) {
            writer.Write(nameof(Width), Width.Value);
        }
        if(Height.HasValue) {
            writer.Write(nameof(Height), Height.Value);
        }
        if(Margin.HasValue) {
            writer.Write(nameof(Margin), Margin.Value);
        }
        if(Padding.HasValue) {
            writer.Write(nameof(Padding), Padding.Value);
        }
        if(HorizontalAlignment.HasValue) {
            writer.WriteEnum(nameof(HorizontalAlignment), HorizontalAlignment.Value);
        }
        if(VerticalAlignment.HasValue) {
            writer.WriteEnum(nameof(VerticalAlignment), VerticalAlignment.Value);
        }
        if(BackgroundColor.HasValue) {
            writer.Write(nameof(BackgroundColor), BackgroundColor.Value);
        }
        if(BorderWidth.HasValue) {
            writer.Write(nameof(BorderWidth), BorderWidth.Value);
        }
        if(BorderRadius.HasValue) {
            writer.Write(nameof(BorderRadius), BorderRadius.Value);
        }
        if(BorderColor.HasValue) {
            writer.Write(nameof(BorderColor), BorderColor.Value);
        }
        writer.WriteEndObject();
        return JsonValueKind.Object;
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

internal readonly record struct LayoutResult
{
    public required RectF Rect { get; init; }
    public required Vector4 BorderRadius { get; init; }
    public required Thickness Padding { get; init; }

    public void Deconstruct(out RectF rect, out Vector4 borderRadius, out Thickness padding)
    {
        rect = Rect;
        borderRadius = BorderRadius;
        padding = Padding;
    }
}

public readonly record struct UIUpdateResult
{
    public required RectF ActualRect { get; init; }
    public required Vector4 ActualBorderWidth { get; init; }
    public required Vector4 ActualBorderRadius { get; init; }
    public required Matrix4 MvpMatrix { get; init; }
    public required Brush BackgroundColor { get; init; }
    public required Brush BorderColor { get; init; }
    public required bool IsHover { get; init; }
}

internal enum PseudoClass
{
    Hover = 0,
    Active = 1,
}

internal struct PseudoClasses
{
    private UIElementPseudoInfo? _hover;
    private UIElementPseudoInfo? _active;

    public void Set(PseudoClass pseudoClass, in ReactSource source)
    {
        GetRef(pseudoClass) = UIElementPseudoInfo.FromJson(source);
    }

    public UIElementPseudoInfo? this[PseudoClass pseudoClass] => GetRef(pseudoClass);

    public bool Has(PseudoClass pseudoClass) => GetRef(pseudoClass).HasValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(PseudoClass pseudoClass, out UIElementPseudoInfo info)
    {
        ref var r = ref GetRef(pseudoClass);
        if(r.HasValue) {
            info = r.Value;
            return true;
        }
        info = default;
        return false;
    }

    [UnscopedRef]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref UIElementPseudoInfo? GetRef(PseudoClass pc)
    {
        switch(pc) {
            case PseudoClass.Hover:
                return ref _hover;
            case PseudoClass.Active:
                return ref _active;
            default:
                throw new ArgumentException(pc.ToString());
        }
    }
}
