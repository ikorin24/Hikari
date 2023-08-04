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

    private UIElementInfo _info;
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
        get => _info.Width;
        set
        {
            if(value == _info.Width) { return; }
            _info.Width = value;
            _needToUpdate |= NeedToUpdateFlags.Layout | NeedToUpdateFlags.Material;
        }
    }
    public LayoutLength Height
    {
        get => _info.Height;
        set
        {
            if(value == _info.Height) { return; }
            _info.Height = value;
            _needToUpdate |= NeedToUpdateFlags.Layout | NeedToUpdateFlags.Material;
        }
    }

    public Thickness Margin
    {
        get => _info.Margin;
        set
        {
            if(value == _info.Margin) { return; }
            _info.Margin = value;
            _needToUpdate |= NeedToUpdateFlags.Layout | NeedToUpdateFlags.Material;
        }
    }

    public Thickness Padding
    {
        get => _info.Padding;
        set
        {
            if(value == _info.Padding) { return; }
            _info.Padding = value;
            _needToUpdate |= NeedToUpdateFlags.Layout | NeedToUpdateFlags.Material;
        }
    }

    public HorizontalAlignment HorizontalAlignment
    {
        get => _info.HorizontalAlignment;
        set
        {
            if(value == _info.HorizontalAlignment) { return; }
            _info.HorizontalAlignment = value;
            _needToUpdate |= NeedToUpdateFlags.Layout | NeedToUpdateFlags.Material;
        }
    }

    public VerticalAlignment VerticalAlignment
    {
        get => _info.VerticalAlignment;
        set
        {
            if(value == _info.VerticalAlignment) { return; }
            _info.VerticalAlignment = value;
            _needToUpdate |= NeedToUpdateFlags.Layout | NeedToUpdateFlags.Material;
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
            _needToUpdate |= NeedToUpdateFlags.Material;
        }
    }

    public Thickness BorderWidth
    {
        get => _info.BorderWidth;
        set
        {
            if(value == _info.BorderWidth) { return; }
            _info.BorderWidth = value;
            _needToUpdate |= NeedToUpdateFlags.Layout | NeedToUpdateFlags.Material;
        }
    }

    public CornerRadius BorderRadius
    {
        get => _info.BorderRadius;
        set
        {
            if(value == _info.BorderRadius) { return; }
            _info.BorderRadius = value;
            _needToUpdate |= NeedToUpdateFlags.Layout | NeedToUpdateFlags.Material;
        }
    }

    public Brush BorderColor
    {
        get => _info.BorderColor;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _info.BorderColor = value;
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
        _info = UIElementInfo.Default;
        Children = new UIElementCollection();
        _needToUpdate = NeedToUpdateFlags.Material | NeedToUpdateFlags.Layout;
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
        // 'rect' is top-left based in Screen
        // When the top-left corner of the UIElement whose size is (200, 100) is placed at (10, 40) in screen,
        // 'rect' is { X = 10, Y = 40, Width = 200, Heigh = 100 }

        var layoutNeedToUpdate = _needToUpdate.HasFlag(NeedToUpdateFlags.Layout);
        LayoutResult layoutResult;
        ContentAreaInfo contentArea;
        var layoutChanged = parentLayoutChanged || layoutNeedToUpdate;
        if(layoutChanged) {
            (layoutResult, contentArea) = Relayout(_info, parentContentArea);
            _needToUpdate &= ~NeedToUpdateFlags.Layout;
            _needToUpdate |= NeedToUpdateFlags.Material;
        }
        else {
            layoutResult = _layoutResult;
            contentArea = new ContentAreaInfo
            {
                Rect = layoutResult.Rect,
                Padding = _info.Padding,
            };
        }
        var isMouseOver = HitTest(mouse.Position, layoutResult.Rect, layoutResult.BorderRadius);
        if(isMouseOver != _isMouseOver) {
            _needToUpdate |= NeedToUpdateFlags.Material;
        }
        _isMouseOverPrev = _isMouseOver;
        _isMouseOver = isMouseOver;
        _layoutResult = layoutResult;
        foreach(var child in _children) {
            child.UpdateLayout(layoutChanged, contentArea, mouse);
        }

        static (LayoutResult, ContentAreaInfo) Relayout(in UIElementInfo info, in ContentAreaInfo parentContentArea)
        {
            var rect = UILayouter.DecideRect(info, parentContentArea);
            var borderRadius = UILayouter.DecideBorderRadius(info.BorderRadius, rect.Size);
            var layoutResult = new LayoutResult
            {
                Rect = rect,
                BorderRadius = borderRadius,
            };
            var contentArea = new ContentAreaInfo
            {
                Rect = rect,
                Padding = info.Padding,
            };
            return (layoutResult, contentArea);
        }
    }

    private static bool HitTest(in Vector2 mousePos, in RectF rect, in Vector4 borderRadius)
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
            var rect = _layoutResult.Rect;
            var borderRadius = _layoutResult.BorderRadius;

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

            var backgroundColor = _info.BackgroundColor;
            var borderWidth = _info.BorderWidth;
            var borderColor = _info.BorderColor;
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
                ActualRect = rect,
                ActualBorderWidth = borderWidth.ToVector4(),
                ActualBorderRadius = borderRadius,
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
}

public record struct UIElementInfo(
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
