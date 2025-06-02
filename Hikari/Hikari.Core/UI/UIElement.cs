#nullable enable
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Hikari.UI;

public abstract partial class UIElement
{
    private FrameObject? _model;
    private UIElement? _parent;
    private readonly UIElementCollection _children;
    private EventSource<UIElement> _modelAlive;
    private EventSource<UIElement> _modelEarlyUpdate;
    private EventSource<UIElement> _modelUpdate;
    private EventSource<UIElement> _modelLateUpdate;
    private EventSource<UIElement> _modelTerminated;
    private EventSource<UIElement> _modelDead;
    private EventSource<UIElement> _clicked;
    private readonly SubscriptionBag _modelSubscriptions = new SubscriptionBag();
    private UIElementInfo _info;
    private LayoutCache? _layoutCache;
    private bool _isClickHolding;
    private bool _needToInvokeClicked;
    private bool _needToLayoutUpdate;

    internal Event<UIElement> ModelAlive => _modelAlive.Event;
    internal Event<UIElement> ModelEarlyUpdate => _modelEarlyUpdate.Event;
    internal Event<UIElement> ModelUpdate => _modelUpdate.Event;
    internal Event<UIElement> ModelLateUpdate => _modelLateUpdate.Event;
    internal Event<UIElement> ModelTerminated => _modelTerminated.Event;
    internal Event<UIElement> ModelDead => _modelDead.Event;
    internal SubscriptionRegister ModelSubscriptions => _modelSubscriptions.Register;

    public Event<UIElement> Clicked => _clicked.Event;
    public Action<UIElement> OnClicked
    {
        init
        {
            Clicked.Subscribe(value);
        }
    }
    public UIElement? Parent => _parent;
    internal FrameObject? Model => _model;
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

    public Color4 Color
    {
        get => _info.Color;
        set
        {
            if(value == _info.Color) { return; }
            _info.Color = value;
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

    protected abstract PseudoInfo? GetHoverProps();
    protected abstract PseudoInfo? GetActiveProps();

    protected UIElement()
    {
        _info = UIElementInfo.Default;
        Children = new UIElementCollection();
        _needToLayoutUpdate = true;

        ModelUpdate.Subscribe(static self =>
        {
            if(self._needToInvokeClicked) {
                self._needToInvokeClicked = false;
                self._clicked.Invoke(self);
            }
        });
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

    private static readonly ConcurrentDictionary<Screen, Own<Mesh>> _cache = new();
    private static Mesh GetMesh(Screen screen)
    {
        return _cache.GetOrAdd(screen, static screen =>
        {
            screen.Closed.Subscribe(static screen =>
            {
                if(_cache.TryRemove(screen, out var cache)) {
                    cache.Dispose();
                }
            });
            return Mesh.Create<VertexSlim, ushort>(
                screen,
                [
                    new VertexSlim(0, 1, 0, 0, 0),
                    new VertexSlim(0, 0, 0, 0, 1),
                    new VertexSlim(1, 0, 0, 1, 1),
                    new VertexSlim(1, 1, 0, 1, 0),
                ],
                [0, 1, 2, 2, 3, 0]);
        }).AsValue();
    }

    internal void CreateModel(UITree tree)
    {
        Debug.Assert(_model == null);
        var material = tree.GetRegisteredMaterial(GetType());
        var model = new FrameObject(GetMesh(tree.Screen), material.AsValue());
        material.DisposeOn(model.Dead);
        model.Alive
            .Subscribe(_ => _modelAlive.Invoke(this))
            .AddTo(model.Subscriptions);
        model.EarlyUpdate
            .Subscribe(_ => _modelEarlyUpdate.Invoke(this))
            .AddTo(model.Subscriptions);
        model.Update
            .Subscribe(_ => _modelUpdate.Invoke(this))
            .AddTo(model.Subscriptions);
        model.LateUpdate
            .Subscribe(_ => _modelLateUpdate.Invoke(this))
            .AddTo(model.Subscriptions);

        model.Terminated.Subscribe(_ =>
        {
            _modelTerminated.Invoke(this);
            foreach(var child in Children) {
                child.Remove();
            }
        }).AddTo(model.Subscriptions);

        model.Dead
            .Subscribe(_ =>
            {
                _modelDead.Invoke(this);
                _modelSubscriptions.Dispose();
            })
            .AddTo(model.Subscriptions);

        _model = model;
        foreach(var child in _children) {
            child.CreateModel(tree);
        }
    }

    internal void RequestRelayout()
    {
        _needToLayoutUpdate = true;
    }

    internal void UpdateLayout(
        bool relayoutRequested, in UIElementInfo parent,
        in RectF parentContentArea, ref FlowLayoutInfo flowInfo,
        Mouse mouse, float scaleFactor)
    {
        // 'rect' is top-left based in Screen
        // When the top-left corner of the UIElement whose size is (200, 100) is placed at (10, 40) in screen,
        // 'rect' is { X = 10, Y = 40, Width = 200, Heigh = 100 }

        var hoverInfo = GetHoverProps();

        var needToRelayout =
            relayoutRequested ||
            _needToLayoutUpdate ||
            (mouse.PositionDelta is null or { IsZero: false } && hoverInfo is not null) ||
            mouse.IsChanged(MouseButton.Left);
        var mousePos = mouse.Position;

        var isHoverPrev = _layoutCache?.IsHover ?? false;
        LayoutCache cache;
        var flags = PseudoFlags.None;
        if(needToRelayout == false && _layoutCache.HasValue) {
            cache = _layoutCache.Value;
        }
        else {
            // First, layout without considering pseudo classes
            cache.AppliedInfo = _info;
            var layout1 = UILayouter.Relayout(cache.AppliedInfo, parent, parentContentArea, flowInfo, scaleFactor, out var flowInfoOut1);

            if(mousePos.HasValue) {
                var isHover1 = HitTest(mousePos.Value, layout1.Rect, layout1.BorderRadius);

                // layout with considering hover pseudo classes if needed
                if(hoverInfo != null) {
                    var mergedInfo = cache.AppliedInfo.Merged(hoverInfo);
                    var layout2 = UILayouter.Relayout(mergedInfo, parent, parentContentArea, flowInfo, scaleFactor, out var flowInfoOut2);
                    var isHover2 = HitTest(mousePos.Value, layout2.Rect, layout2.BorderRadius);
                    cache = (isHover1, isHover2) switch
                    {
                        (true, true) => new LayoutCache
                        {
                            IsHover = true,
                            Layout = layout2,
                            AppliedInfo = mergedInfo,
                            FlowInfo = flowInfoOut2,
                        },
                        (false, false) => new LayoutCache
                        {
                            IsHover = false,
                            Layout = layout1,
                            AppliedInfo = cache.AppliedInfo,
                            FlowInfo = flowInfoOut1,
                        },
                        _ => new LayoutCache
                        {
                            IsHover = isHoverPrev,
                            Layout = isHoverPrev ? layout2 : layout1,
                            AppliedInfo = isHoverPrev ? mergedInfo : cache.AppliedInfo,
                            FlowInfo = isHoverPrev ? flowInfoOut2 : flowInfoOut1,
                        },
                    };

                }
                else {
                    cache.IsHover = isHover1;
                    cache.Layout = layout1;
                    cache.FlowInfo = flowInfoOut1;
                }
            }
            else {
                cache.IsHover = false;
                cache.Layout = layout1;
                cache.FlowInfo = flowInfoOut1;
            }
        }

        if(cache.IsHover) {
            flags |= PseudoFlags.Hover;
        }

        // set or clear flag for click holding
        if(_isClickHolding && mouse.IsUp(MouseButton.Left)) {
            _isClickHolding = false;
            if(cache.IsHover) {
                _needToInvokeClicked = true;
            }
        }
        else if(cache.IsHover && mouse.IsDown(MouseButton.Left)) {
            _isClickHolding = true;
        }

        // overwrite layout if 'active'
        var activeInfo = GetActiveProps();
        if(_isClickHolding) {
            flags |= PseudoFlags.Active;
            if(activeInfo is not null) {
                cache.AppliedInfo = cache.AppliedInfo.Merged(activeInfo);
                if(activeInfo.HasLayoutInfo) {
                    cache.Layout = UILayouter.Relayout(cache.AppliedInfo, parent, parentContentArea, flowInfo, scaleFactor, out cache.FlowInfo);
                }
            }
        }

        _needToLayoutUpdate = false;
        var requestChildRelayout = _layoutCache switch
        {
            null => true,
            _ =>
                _layoutCache.Value.Layout.Rect != cache.Layout.Rect ||
                _layoutCache.Value.AppliedInfo.Padding != cache.AppliedInfo.Padding ||
                _layoutCache.Value.AppliedInfo.Flow != cache.AppliedInfo.Flow,
        };
        flowInfo = cache.FlowInfo;
        _layoutCache = cache;
        OnUpdateLayout(flags, scaleFactor);

        var padding = new Thickness
        {
            Top = cache.AppliedInfo.Padding.Top * scaleFactor,
            Right = cache.AppliedInfo.Padding.Right * scaleFactor,
            Bottom = cache.AppliedInfo.Padding.Bottom * scaleFactor,
            Left = cache.AppliedInfo.Padding.Left * scaleFactor,
        };
        var contentArea = new RectF
        {
            X = cache.Layout.Rect.X + padding.Left,
            Y = cache.Layout.Rect.Y + padding.Top,
            Width = float.Max(0, cache.Layout.Rect.Width - padding.Left - padding.Right),
            Height = float.Max(0, cache.Layout.Rect.Height - padding.Top - padding.Bottom),
        };

        var childrenFlowInfo = cache.AppliedInfo.Flow.NewChildrenFlowInfo(contentArea);

        Debug.Assert(_layoutCache.HasValue);
        ref readonly var appliedInfoRef = ref _layoutCache.ValueRef().AppliedInfo;
        foreach(var child in _children) {
            child.UpdateLayout(requestChildRelayout, appliedInfoRef, contentArea, ref childrenFlowInfo, mouse, scaleFactor);
        }
    }

    protected abstract void OnUpdateLayout(PseudoFlags flags, float scaleFactor);

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

    internal void UpdateMaterial(in Vector2u screenSize, float scaleFactor, in Matrix4 uiProjection, float depth)
    {
        var model = _model;
        Debug.Assert(model != null);
        if(model == null) { return; }

        Debug.Assert(_layoutCache != null);
        ref readonly var cache = ref _layoutCache.ValueRef();

        var shadowWidth = (cache.AppliedInfo.BoxShadow.BlurRadius + cache.AppliedInfo.BoxShadow.SpreadRadius) * scaleFactor;
        var shadowRect = new RectF
        {
            X = cache.Layout.Rect.X - shadowWidth + cache.AppliedInfo.BoxShadow.OffsetX * scaleFactor,
            Y = cache.Layout.Rect.Y - shadowWidth + cache.AppliedInfo.BoxShadow.OffsetY * scaleFactor,
            Width = cache.Layout.Rect.Width + shadowWidth * 2f,
            Height = cache.Layout.Rect.Height + shadowWidth * 2f,
        };

        var polygonRect = cache.Layout.Rect.GetMargedRect(shadowRect);
        var modelOrigin = new Vector3
        {
            // origin is bottom-left of rect because clip space is bottom-left based
            X = polygonRect.Position.X,
            Y = screenSize.Y - polygonRect.Position.Y - polygonRect.Size.Y,
            Z = depth,
        };
        var modelMatrix =
            modelOrigin.ToTranslationMatrix4() *
            new Matrix4(
                new Vector4(polygonRect.Size.X, 0, 0, 0),
                new Vector4(0, polygonRect.Size.Y, 0, 0),
                new Vector4(0, 0, 1, 0),
                new Vector4(0, 0, 0, 1));

        var renderer = model.Renderer;
        Debug.Assert(renderer != null);
        Debug.Assert(renderer.SubrendererCount == 1);
        var material = renderer.GetMaterial<IUIMaterial>(0);
        material.UpdateMaterial(this, cache, uiProjection * modelMatrix, scaleFactor);
    }
}

internal record struct UIElementInfo
{
    public required LayoutLength Width;
    public required LayoutLength Height;
    public required Thickness Margin;
    public required Thickness Padding;
    public required HorizontalAlignment HorizontalAlignment;
    public required VerticalAlignment VerticalAlignment;
    public required Brush Background;
    public required Thickness BorderWidth;
    public required CornerRadius BorderRadius;
    public required Brush BorderColor;
    public required BoxShadow BoxShadow;
    public required Flow Flow;
    public required Color4 Color;

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
        Color = DefaultColor,
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
    internal static Color4 DefaultColor => Color4.Black;

    internal readonly UIElementInfo Merged(PseudoInfo p)
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
            Color = p.Color ?? Color,
        };
    }
}

public abstract partial record PseudoInfo
{
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
    public Color4? Color { get; init; }

    public bool HasLayoutInfo =>
        Width.HasValue || Height.HasValue || Margin.HasValue ||
        Padding.HasValue || HorizontalAlignment.HasValue || VerticalAlignment.HasValue ||
        BorderWidth.HasValue;

    internal const string HoverName = "&:Hover";
    internal const string ActiveName = "&:Active";
}

[Flags]
public enum PseudoFlags : uint
{
    None = 0,
    Hover = (1 << 0),
    Active = (1 << 1),
}

internal record struct LayoutCache
{
    public LayoutResult Layout;
    public bool IsHover;
    public UIElementInfo AppliedInfo;
    public FlowLayoutInfo FlowInfo;
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
