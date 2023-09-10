#nullable enable
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Hikari.UI;

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
    private PseudoInfo? _hoverInfo;
    private PseudoInfo? _activeInfo;

    private (LayoutResult Layout, UIElementInfo AppliedInfo, Vector2 FlowHead)? _layoutCache;
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

    public Brush Background
    {
        get => _info.Background;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if(value == _info.Background) { return; }
            _info.Background = value;
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
            if(value == _info.BorderColor) { return; }
            _info.BorderColor = value;
        }
    }

    public BoxShadow BoxShadow
    {
        get => _info.BoxShadow;
        set
        {
            if(value == _info.BoxShadow) { return; }
            _info.BoxShadow = value;
        }
    }

    public Flow Flow
    {
        get => _info.Flow;
        set
        {
            if(value == _info.Flow) { return; }
            _info.Flow = value;
            _needToLayoutUpdate = true;
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

    public PseudoInfo? HoverProps
    {
        get => _hoverInfo;
        set
        {
            if(_hoverInfo == value) { return; }
            _hoverInfo = value;
            _needToLayoutUpdate = true;
        }
    }

    public PseudoInfo? ActiveProps
    {
        get => _activeInfo;
        set
        {
            if(_activeInfo == value) { return; }
            _activeInfo = value;
            _needToLayoutUpdate = true;
        }
    }

    protected UIElement()
    {
        _info = UIElementInfo.Default;
        _hoverInfo = null;
        _activeInfo = null;
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

    protected UIElement(in ObjectSource source) : this()
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
        if(source.TryGetProperty(nameof(Background), out var backgroundColor)) {
            _info.Background = Brush.FromJson(backgroundColor);
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
        if(source.TryGetProperty(nameof(BoxShadow), out var boxShadow)) {
            _info.BoxShadow = BoxShadow.FromJson(boxShadow);
        }
        if(source.TryGetProperty(nameof(Flow), out var flow)) {
            _info.Flow = Flow.FromJson(flow);
        }
        if(source.TryGetProperty(nameof(Clicked), out var clicked)) {
            var action = clicked.Instantiate<Action<UIElement>>();
            _clicked.Event.Subscribe(action);
        }


        foreach(var (name, value) in source.EnumerateProperties()) {
            if(name.StartsWith("&:")) {
                var pseudo = name.AsSpan(2);
                switch(pseudo) {
                    case "Hover": {
                        _hoverInfo = PseudoInfo.FromJson(value);
                        break;
                    }
                    case "Active": {
                        _activeInfo = PseudoInfo.FromJson(value);
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
        writer.Write(nameof(Background), _info.Background);
        writer.Write(nameof(BorderWidth), _info.BorderWidth);
        writer.Write(nameof(BorderRadius), _info.BorderRadius);
        writer.Write(nameof(BorderColor), _info.BorderColor);
        writer.Write(nameof(BoxShadow), _info.BoxShadow);
        writer.Write(nameof(Flow), _info.Flow);
        writer.Write(nameof(Children), _children);
    }

    void IReactive.ApplyDiff(in ObjectSource source)
    {
        ApplyDiffProtected(source);
    }

    protected virtual void ApplyDiffProtected(in ObjectSource source)
    {
        Width = source.ApplyProperty(nameof(Width), Width, () => UIElementInfo.DefaultWidth, out _);
        Height = source.ApplyProperty(nameof(Height), Height, () => UIElementInfo.DefaultHeight, out _);
        Margin = source.ApplyProperty(nameof(Margin), Margin, () => UIElementInfo.DefaultMargin, out _);
        Padding = source.ApplyProperty(nameof(Padding), Padding, () => UIElementInfo.DefaultPadding, out _);
        HorizontalAlignment = source.ApplyProperty(nameof(HorizontalAlignment), HorizontalAlignment, () => UIElementInfo.DefaultHorizontalAlignment, out _);
        VerticalAlignment = source.ApplyProperty(nameof(VerticalAlignment), VerticalAlignment, () => UIElementInfo.DefaultVerticalAlignment, out _);
        Background = source.ApplyProperty(nameof(Background), Background, () => UIElementInfo.DefaultBackground, out _);
        BorderWidth = source.ApplyProperty(nameof(BorderWidth), BorderWidth, () => UIElementInfo.DefaultBorderWidth, out _);
        BorderRadius = source.ApplyProperty(nameof(BorderRadius), BorderRadius, () => UIElementInfo.DefaultBorderRadius, out _);
        BorderColor = source.ApplyProperty(nameof(BorderColor), BorderColor, () => UIElementInfo.DefaultBorderColor, out _);
        BoxShadow = source.ApplyProperty(nameof(BoxShadow), BoxShadow, () => UIElementInfo.DefaultBoxShadow, out _);
        Flow = source.ApplyProperty(nameof(Flow), Flow, () => UIElementInfo.DefaultFlow, out _);

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

    internal void OnChildrenChanged()
    {
        _needToLayoutUpdate = true;
    }

    internal LayoutResult UpdateLayout(bool relayoutRequested, in UIElementInfo parent, in RectF parentContentArea, ref Vector2 flowHead, Mouse mouse)
    {
        // 'rect' is top-left based in Screen
        // When the top-left corner of the UIElement whose size is (200, 100) is placed at (10, 40) in screen,
        // 'rect' is { X = 10, Y = 40, Width = 200, Heigh = 100 }

        var needToRelayout =
            relayoutRequested ||
            _needToLayoutUpdate ||
            (mouse.PositionDelta?.IsZero == false && _hoverInfo != null) ||
            mouse.IsChanged(MouseButton.Left);
        var mousePos = mouse.Position;

        var isHoverPrev = _isHover;
        UIElementInfo appliedInfo;
        LayoutResult layout;
        bool isHover;
        Vector2 flowHeadOut;
        if(needToRelayout == false && _layoutCache.HasValue) {
            (layout, appliedInfo, flowHeadOut) = _layoutCache.Value;
            isHover = _isHover;
        }
        else {
            // First, layout without considering pseudo classes
            appliedInfo = _info;
            var layout1 = Relayout(appliedInfo, parent, parentContentArea, flowHead, out var flowHeadOut1);

            if(mousePos.HasValue) {
                var isHover1 = HitTest(mousePos.Value, layout1.Rect, layout1.BorderRadius);

                // layout with considering hover pseudo classes if needed
                if(_hoverInfo != null) {
                    var mergedInfo = appliedInfo.Merged(_hoverInfo.Value);
                    var layout2 = Relayout(mergedInfo, parent, parentContentArea, flowHead, out var flowHeadOut2);
                    var isHover2 = HitTest(mousePos.Value, layout2.Rect, layout2.BorderRadius);
                    (isHover, layout, appliedInfo, flowHeadOut) = (isHover1, isHover2) switch
                    {
                        (true, true) => (true, layout2, mergedInfo, flowHeadOut2),
                        (false, false) => (false, layout1, appliedInfo, flowHeadOut1),
                        _ => isHoverPrev ? (true, layout2, mergedInfo, flowHeadOut2) : (false, layout1, appliedInfo, flowHeadOut1),
                    };
                }
                else {
                    (isHover, layout, flowHeadOut) = (isHover1, layout1, flowHeadOut1);
                }
            }
            else {
                (isHover, layout, flowHeadOut) = (false, layout1, flowHeadOut1);
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
        if(_isClickHolding && _activeInfo != null) {
            appliedInfo = appliedInfo.Merged(_activeInfo.Value);
            if(_activeInfo.Value.HasLayoutInfo) {
                layout = Relayout(appliedInfo, parent, parentContentArea, flowHead, out flowHeadOut);
            }
        }

        _needToLayoutUpdate = false;
        var requestChildRelayout = _layoutCache switch
        {
            null => true,
            _ =>
                _layoutCache.Value.Layout.Rect != layout.Rect ||
                _layoutCache.Value.AppliedInfo.Padding != appliedInfo.Padding ||
                _layoutCache.Value.AppliedInfo.Flow != appliedInfo.Flow,
        };
        _isHover = isHover;
        flowHead = flowHeadOut;
        _layoutCache = (layout, appliedInfo, flowHead);
        var contentArea = new RectF
        {
            X = layout.Rect.X + appliedInfo.Padding.Left,
            Y = layout.Rect.Y + appliedInfo.Padding.Top,
            Width = float.Max(0, layout.Rect.Width - appliedInfo.Padding.Left - appliedInfo.Padding.Right),
            Height = float.Max(0, layout.Rect.Height - appliedInfo.Padding.Top - appliedInfo.Padding.Bottom),
        };

        var childFlowHead = appliedInfo.Flow.Direction switch
        {
            FlowDirection.Row => new Vector2(contentArea.X, contentArea.Y),
            FlowDirection.Column => new Vector2(contentArea.X, contentArea.Y),
            FlowDirection.RowReverse => new Vector2(contentArea.X + contentArea.Width, contentArea.Y),
            FlowDirection.ColumnReverse => new Vector2(contentArea.X, contentArea.Y + contentArea.Height),
            FlowDirection.None or _ => Vector2.Zero,
        };

        Debug.Assert(_layoutCache.HasValue);
        ref readonly var appliedInfoRef = ref _layoutCache.ValueRef().AppliedInfo;
        foreach(var child in _children) {
            child.UpdateLayout(requestChildRelayout, appliedInfoRef, contentArea, ref childFlowHead, mouse);
        }
        return layout;
    }

    private static LayoutResult Relayout(in UIElementInfo info, in UIElementInfo parent, in RectF parentContentArea, in Vector2 flowHead, out Vector2 flowHeadOut)
    {
        flowHeadOut = flowHead;
        var rect = UILayouter.DecideRect(info, parent, parentContentArea, ref flowHeadOut);
        var borderRadius = UILayouter.DecideBorderRadius(info.BorderRadius, rect.Size);
        var layoutResult = new LayoutResult
        {
            Rect = rect,
            BorderRadius = borderRadius,
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

    internal void UpdateMaterial(in Vector2u screenSize, in Matrix4 uiProjection, uint index)
    {
        var model = _model;
        Debug.Assert(model != null);
        if(model == null) { return; }

        Debug.Assert(_layoutCache != null);
        var ((rect, borderRadius), appliedInfo, _) = _layoutCache.Value;
        var shadowWidth = (appliedInfo.BoxShadow.BlurRadius + appliedInfo.BoxShadow.SpreadRadius);
        var shadowRect = new RectF
        {
            X = rect.X - shadowWidth + appliedInfo.BoxShadow.OffsetX,
            Y = rect.Y - shadowWidth + appliedInfo.BoxShadow.OffsetY,
            Width = rect.Width + shadowWidth * 2f,
            Height = rect.Height + shadowWidth * 2f,
        };

        var polygonRect = rect.GetMargedRect(shadowRect);
        var depth = float.Max(0, 1f - (float)index / 100000f);
        var modelOrigin = new Vector3
        {
            // origin is bottom-left of rect because clip space is bottom-left based
            X = polygonRect.Position.X,
            Y = screenSize.Y - polygonRect.Position.Y - polygonRect.Size.Y,
            Z = -depth,
        };
        var modelMatrix =
            modelOrigin.ToTranslationMatrix4() *
            new Matrix4(
                new Vector4(polygonRect.Size.X, 0, 0, 0),
                new Vector4(0, polygonRect.Size.Y, 0, 0),
                new Vector4(0, 0, 1, 0),
                new Vector4(0, 0, 0, 1));
        var result = new UIUpdateResult
        {
            ActualRect = rect,
            ActualBorderWidth = appliedInfo.BorderWidth.ToVector4(),
            ActualBorderRadius = borderRadius,
            MvpMatrix = uiProjection * modelMatrix,
            Background = appliedInfo.Background,
            BorderColor = appliedInfo.BorderColor,
            BoxShadow = appliedInfo.BoxShadow,
            IsHover = _isHover,
        };
        model.Material.UpdateMaterial(this, result);
    }
}

internal record struct UIElementInfo
{
    public LayoutLength Width { get; set; }
    public LayoutLength Height { get; set; }
    public Thickness Margin { get; set; }
    public Thickness Padding { get; set; }
    public HorizontalAlignment HorizontalAlignment { get; set; }
    public VerticalAlignment VerticalAlignment { get; set; }
    public Brush Background { get; set; }
    public Thickness BorderWidth { get; set; }
    public CornerRadius BorderRadius { get; set; }
    public Brush BorderColor { get; set; }
    public BoxShadow BoxShadow { get; set; }
    public Flow Flow { get; set; }

    internal static UIElementInfo Default => new()
    {
        Width = DefaultWidth,
        Height = DefaultHeight,
        Margin = DefaultMargin,
        Padding = DefaultPadding,
        HorizontalAlignment = DefaultHorizontalAlignment,
        VerticalAlignment = DefaultVerticalAlignment,
        Background = DefaultBackground,
        BorderWidth = DefaultBorderWidth,
        BorderRadius = DefaultBorderRadius,
        BorderColor = DefaultBorderColor,
        BoxShadow = DefaultBoxShadow,
        Flow = DefaultFlow,
    };

    internal static LayoutLength DefaultWidth => new LayoutLength(1f, LayoutLengthType.Proportion);
    internal static LayoutLength DefaultHeight => new LayoutLength(1f, LayoutLengthType.Proportion);
    internal static Thickness DefaultMargin => new Thickness(0f);
    internal static Thickness DefaultPadding => new Thickness(0f);
    internal static HorizontalAlignment DefaultHorizontalAlignment => HorizontalAlignment.Center;
    internal static VerticalAlignment DefaultVerticalAlignment => VerticalAlignment.Center;
    internal static Brush DefaultBackground => Brush.White;
    internal static Thickness DefaultBorderWidth => new Thickness(0f);
    internal static CornerRadius DefaultBorderRadius => CornerRadius.Zero;
    internal static Brush DefaultBorderColor => Brush.Black;
    internal static BoxShadow DefaultBoxShadow => BoxShadow.None;
    internal static Flow DefaultFlow => Flow.Default;

    internal readonly UIElementInfo Merged(in PseudoInfo p)
    {
        return new()
        {
            Width = p.Width ?? Width,
            Height = p.Height ?? Height,
            Margin = p.Margin ?? Margin,
            Padding = p.Padding ?? Padding,
            HorizontalAlignment = p.HorizontalAlignment ?? HorizontalAlignment,
            VerticalAlignment = p.VerticalAlignment ?? VerticalAlignment,
            Background = p.Background ?? Background,
            BorderWidth = p.BorderWidth ?? BorderWidth,
            BorderRadius = p.BorderRadius ?? BorderRadius,
            BorderColor = p.BorderColor ?? BorderColor,
            BoxShadow = p.BoxShadow ?? BoxShadow,
            Flow = p.Flow ?? Flow,
        };
    }
}

public readonly record struct PseudoInfo
    : IFromJson<PseudoInfo>,
      IToJson
{
    public readonly record struct Prop(string PropName, ObjectSource Value);

    static PseudoInfo() => Serializer.RegisterConstructor(FromJson);

    private static readonly ImmutableArray<Prop> EmptyEx = ImmutableArray<Prop>.Empty;

    public LayoutLength? Width { get; init; }
    public LayoutLength? Height { get; init; }
    public Thickness? Margin { get; init; }
    public Thickness? Padding { get; init; }
    public HorizontalAlignment? HorizontalAlignment { get; init; }
    public VerticalAlignment? VerticalAlignment { get; init; }
    public Brush? Background { get; init; }
    public Thickness? BorderWidth { get; init; }
    public CornerRadius? BorderRadius { get; init; }
    public Brush? BorderColor { get; init; }
    public BoxShadow? BoxShadow { get; init; }
    public Flow? Flow { get; init; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly ImmutableArray<Prop> _ex;
    public ImmutableArray<Prop> Ex
    {
        get => _ex.IsDefault ? EmptyEx : _ex;
        init => _ex = value.IsDefault ? EmptyEx : value;
    }

    public bool HasLayoutInfo =>
        Width.HasValue || Height.HasValue || Margin.HasValue ||
        Padding.HasValue || HorizontalAlignment.HasValue || VerticalAlignment.HasValue ||
        BorderWidth.HasValue;

    public bool TryGetEx<T>(string propName, [MaybeNullWhen(false)] out T value)
    {
        foreach(var (name, v) in Ex) {
            if(name == propName) {
                value = v.Instantiate<T>();
                return true;
            }
        }
        value = default;
        return false;
    }

    public static PseudoInfo FromJson(in ObjectSource source)
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
        BoxShadow? boxShadow = null;
        Flow? flow = null;
        var ex = ImmutableArray.CreateBuilder<Prop>();

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
                case nameof(Background): {
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
                case nameof(BoxShadow): {
                    boxShadow = value.Instantiate<BoxShadow>();
                    break;
                }
                case nameof(Flow): {
                    flow = value.Instantiate<Flow>();
                    break;
                }
                default: {
                    ex.Add(new(name, value));
                    break;
                }
            }
        }

        return new PseudoInfo
        {
            Width = width,
            Height = height,
            Margin = margin,
            Padding = padding,
            HorizontalAlignment = horizontalAlignment,
            VerticalAlignment = verticalAlignment,
            Background = backgroundColor,
            BorderWidth = borderWidth,
            BorderRadius = borderRadius,
            BorderColor = borderColor,
            BoxShadow = boxShadow,
            Flow = flow,
            Ex = ex.Count == 0 ? EmptyEx : ex.ToImmutable(),
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
        if(Background.HasValue) {
            writer.Write(nameof(Background), Background.Value);
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
        if(BoxShadow.HasValue) {
            writer.Write(nameof(BoxShadow), BoxShadow.Value);
        }
        writer.WriteEndObject();
        return JsonValueKind.Object;
    }
}

internal readonly record struct LayoutResult
{
    public required RectF Rect { get; init; }
    public required Vector4 BorderRadius { get; init; }

    public void Deconstruct(out RectF rect, out Vector4 borderRadius)
    {
        rect = Rect;
        borderRadius = BorderRadius;
    }
}

public readonly record struct UIUpdateResult
{
    public required RectF ActualRect { get; init; }
    public required Vector4 ActualBorderWidth { get; init; }
    public required Vector4 ActualBorderRadius { get; init; }
    public required Matrix4 MvpMatrix { get; init; }
    public required Brush Background { get; init; }
    public required Brush BorderColor { get; init; }
    public required BoxShadow BoxShadow { get; init; }
    public required bool IsHover { get; init; }
}
