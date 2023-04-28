#nullable enable
using Elffy.Effective;
using Elffy.NativeBind;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy;

public sealed class RenderPipeline : IScreenManaged
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.RenderPipeline> _native;
    internal Rust.Ref<Wgpu.RenderPipeline> NativeRef => _native.Unwrap();

    public Screen Screen => _screen;

    public bool IsManaged => _native.IsNone == false;

    private RenderPipeline(Screen screen, Rust.Box<Wgpu.RenderPipeline> native)
    {
        _screen = screen;
        _native = native;
    }

    ~RenderPipeline() => Release(false);

    public void Validate() => IScreenManaged.DefaultValidate(this);

    private void Release()
    {
        Release(true);
        GC.SuppressFinalize(this);
    }

    private void Release(bool disposing)
    {
        if(InterlockedEx.Exchange(ref _native, Rust.OptionBox<Wgpu.RenderPipeline>.None).IsSome(out var native)) {
            native.DestroyRenderPipeline();
            if(disposing) {
            }
        }
    }

    public unsafe static Own<RenderPipeline> Create(Screen screen, in RenderPipelineDescriptor desc)
    {
        var pins = new PinHandleHolder();
        try {
            var descNative = desc.ToNative(pins);
            var renderPipelineNative = screen.AsRefChecked().CreateRenderPipeline(descNative);
            var renderPipeline = new RenderPipeline(screen, renderPipelineNative);
            return Own.New(renderPipeline, static x => SafeCast.As<RenderPipeline>(x).Release());
        }
        finally {
            pins.Dispose();
        }
    }
}

public readonly struct RenderPipelineDescriptor
{
    private readonly PipelineLayout _layout;

    public required PipelineLayout Layout
    {
        get => _layout;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _layout = value;
        }
    }
    public required VertexState Vertex { get; init; }
    public required FragmentState? Fragment { get; init; }
    public required PrimitiveState Primitive { get; init; }
    public required DepthStencilState? DepthStencil { get; init; }
    public required MultisampleState Multisample { get; init; }
    public required u32 Multiview { get; init; }

    internal CE.RenderPipelineDescriptor ToNative(PinHandleHolder pins)
    {
        return new CE.RenderPipelineDescriptor
        {
            layout = Layout.NativeRef,
            vertex = Vertex.ToNative(pins),
            fragment = Fragment.ToNative(fragment => fragment.ToNative(pins)),
            primitive = Primitive.ToNative(),
            depth_stencil = DepthStencil.ToNative(x => x.ToNative()),
            multisample = Multisample.ToNative(),
            multiview = Multiview,
        };
    }
}

public readonly struct VertexState
{
    public required ShaderModule Module { get; init; }
    public required ReadOnlyMemory<byte> EntryPoint { get; init; }
    public required ReadOnlyMemory<VertexBufferLayout> Buffers { get; init; }

    internal CE.VertexState ToNative(PinHandleHolder pins)
    {
        return new CE.VertexState
        {
            module = Module.NativeRef,
            entry_point = EntryPoint.AsFixedSlice(pins),
            buffers = Buffers.SelectToArray(pins, static (x, pins) => x.ToNative(pins)).AsFixedSlice(pins),
        };
    }
}

public readonly struct FragmentState
{
    public required ShaderModule Module { get; init; }
    public required ReadOnlyMemory<byte> EntryPoint { get; init; }
    public required ReadOnlyMemory<ColorTargetState?> Targets { get; init; }

    internal CE.FragmentState ToNative(PinHandleHolder pins)
    {
        return new CE.FragmentState
        {
            module = Module.NativeRef,
            entry_point = EntryPoint.AsFixedSlice(pins),
            targets = Targets.SelectToArray(static target => target.ToNative(static x => x.ToNative())).AsFixedSlice(pins),
        };
    }
}

public readonly struct PrimitiveState
{
    public required PrimitiveTopology Topology { get; init; }
    public required IndexFormat? StripIndexFormat { get; init; }
    public required FrontFace FrontFace { get; init; }
    public required Face? CullMode { get; init; }
    public required PolygonMode PolygonMode { get; init; }

    internal CE.PrimitiveState ToNative()
    {
        return new CE.PrimitiveState
        {
            topology = Topology.MapOrThrow(),
            strip_index_format = StripIndexFormat.ToNative(x => x.MapOrThrow()),
            front_face = FrontFace.MapOrThrow(),
            cull_mode = CullMode.ToNative(x => x.MapOrThrow()),
            polygon_mode = PolygonMode.MapOrThrow(),
        };
    }
}

public readonly struct DepthStencilState
{
    public required TextureFormat Format { get; init; }
    public required bool DepthWriteEnabled { get; init; }
    public required CompareFunction DepthCompare { get; init; }
    public required StencilState Stencil { get; init; }
    public required DepthBiasState Bias { get; init; }

    internal CE.DepthStencilState ToNative()
    {
        return new CE.DepthStencilState
        {
            format = Format.MapOrThrow(),
            depth_write_enabled = DepthWriteEnabled,
            depth_compare = DepthCompare.MapOrThrow(),
            stencil = Stencil.ToNative(),
            bias = Bias.ToNative(),
        };
    }
}

public readonly struct StencilState
{
    public required StencilFaceState Front { get; init; }
    public required StencilFaceState Back { get; init; }
    public required u32 ReadMask { get; init; }
    public required u32 WriteMask { get; init; }

    public static StencilState Default => new()
    {
        Front = StencilFaceState.Default,
        Back = StencilFaceState.Default,
        ReadMask = 0,
        WriteMask = 0,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Wgpu.StencilState ToNative()
    {
        return new Wgpu.StencilState
        {
            front = Front.ToNative(),
            back = Back.ToNative(),
            read_mask = ReadMask,
            write_mask = WriteMask,
        };
    }
}

public readonly struct StencilFaceState
{
    public required CompareFunction Compare { get; init; }
    public required StencilOperation FailOp { get; init; }
    public required StencilOperation DepthFailOp { get; init; }
    public required StencilOperation PassOp { get; init; }

    public static StencilFaceState Default => Ignore;

    public static StencilFaceState Ignore => new()
    {
        Compare = CompareFunction.Always,
        FailOp = StencilOperation.Keep,
        DepthFailOp = StencilOperation.Keep,
        PassOp = StencilOperation.Keep,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Wgpu.StencilFaceState ToNative()
    {
        return new Wgpu.StencilFaceState
        {
            compare = Compare.MapOrThrow(),
            fail_op = FailOp.MapOrThrow(),
            depth_fail_op = DepthFailOp.MapOrThrow(),
            pass_op = PassOp.MapOrThrow(),
        };
    }
}

public readonly struct DepthBiasState
{
    public required i32 Constant { get; init; }
    public required f32 SlopeScale { get; init; }
    public required f32 Clamp { get; init; }

    public static DepthBiasState Default => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Wgpu.DepthBiasState ToNative()
    {
        return new Wgpu.DepthBiasState
        {
            constant = Constant,
            slope_scale = SlopeScale,
            clamp = Clamp,
        };
    }
}

public readonly struct MultisampleState
{
    public required u32 Count { get; init; }
    public required u64 Mask { get; init; }
    public required bool AlphaToCoverageEnabled { get; init; }

    public static MultisampleState Default => new()
    {
        Count = 1,
        Mask = 0xffff_ffff_ffff_ffff,
        AlphaToCoverageEnabled = false,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Wgpu.MultisampleState ToNative()
    {
        return new Wgpu.MultisampleState
        {
            count = Count,
            mask = Mask,
            alpha_to_coverage_enabled = AlphaToCoverageEnabled,
        };
    }
}

public enum StencilOperation
{
    [EnumMapTo(Wgpu.StencilOperation.Keep)] Keep = 0,
    [EnumMapTo(Wgpu.StencilOperation.Zero)] Zero = 1,
    [EnumMapTo(Wgpu.StencilOperation.Replace)] Replace = 2,
    [EnumMapTo(Wgpu.StencilOperation.Invert)] Invert = 3,
    [EnumMapTo(Wgpu.StencilOperation.IncrementClamp)] IncrementClamp = 4,
    [EnumMapTo(Wgpu.StencilOperation.DecrementClamp)] DecrementClamp = 5,
    [EnumMapTo(Wgpu.StencilOperation.IncrementWrap)] IncrementWrap = 6,
    [EnumMapTo(Wgpu.StencilOperation.DecrementWrap)] DecrementWrap = 7,
}

public enum CompareFunction
{
    [EnumMapTo(Wgpu.CompareFunction.Never)] Never = 1,
    [EnumMapTo(Wgpu.CompareFunction.Less)] Less = 2,
    [EnumMapTo(Wgpu.CompareFunction.Equal)] Equal = 3,
    [EnumMapTo(Wgpu.CompareFunction.LessEqual)] LessEqual = 4,
    [EnumMapTo(Wgpu.CompareFunction.Greater)] Greater = 5,
    [EnumMapTo(Wgpu.CompareFunction.NotEqual)] NotEqual = 6,
    [EnumMapTo(Wgpu.CompareFunction.GreaterEqual)] GreaterEqual = 7,
    [EnumMapTo(Wgpu.CompareFunction.Always)] Always = 8,
}


public enum PrimitiveTopology
{
    [EnumMapTo(Wgpu.PrimitiveTopology.PointList)] PointList = 0,
    [EnumMapTo(Wgpu.PrimitiveTopology.LineList)] LineList = 1,
    [EnumMapTo(Wgpu.PrimitiveTopology.LineStrip)] LineStrip = 2,
    [EnumMapTo(Wgpu.PrimitiveTopology.TriangleList)] TriangleList = 3,
    [EnumMapTo(Wgpu.PrimitiveTopology.TriangleStrip)] TriangleStrip = 4,
}

public enum IndexFormat
{
    [EnumMapTo(Wgpu.IndexFormat.Uint16)] Uint16 = 0,
    [EnumMapTo(Wgpu.IndexFormat.Uint32)] Uint32 = 1,
}

public enum FrontFace
{
    [EnumMapTo(Wgpu.FrontFace.Ccw)] Ccw = 0,
    [EnumMapTo(Wgpu.FrontFace.Cw)] Cw = 1,
}

public enum Face
{
    [EnumMapTo(Wgpu.Face.Front)] Front = 0,
    [EnumMapTo(Wgpu.Face.Back)] Back = 1,
}

public enum PolygonMode
{
    [EnumMapTo(Wgpu.PolygonMode.Fill)] Fill = 0,
    [EnumMapTo(Wgpu.PolygonMode.Line)] Line = 1,
    [EnumMapTo(Wgpu.PolygonMode.Point)] Point = 2,
}

public readonly struct ColorTargetState
{
    public required TextureFormat Format { get; init; }
    public required BlendState? Blend { get; init; }
    public required ColorWrites WriteMask { get; init; }

    internal CE.ColorTargetState ToNative()
    {
        return new CE.ColorTargetState
        {
            format = Format.MapOrThrow(),
            blend = Blend.ToNative(blend => blend.ToNative()),
            write_mask = WriteMask.FlagsMap(),
        };
    }
}

public readonly struct VertexBufferLayout
{
    public required u64 ArrayStride { get; init; }
    public required VertexStepMode StepMode { get; init; }
    public required ReadOnlyMemory<VertexAttr> Attributes { get; init; }

    internal CE.VertexBufferLayout ToNative(PinHandleHolder pins)
    {
        return new CE.VertexBufferLayout
        {
            array_stride = ArrayStride,
            step_mode = StepMode.MapOrThrow(),
            attributes = Attributes.SelectToArray(static x => x.ToNative()).AsFixedSlice(pins),
        };
    }

    public static VertexBufferLayout FromVertex<TVertex>(ReadOnlySpan<(int Location, VertexFieldSemantics Semantics)> mapping)
        where TVertex : unmanaged, IVertex
    {
        return FromVertex<TVertex>(mapping.MarshalCast<(int Location, VertexFieldSemantics Semantics), (uint Location, VertexFieldSemantics Semantics)>());
    }

    public static VertexBufferLayout FromVertex<TVertex>(ReadOnlySpan<(uint Location, VertexFieldSemantics Semantics)> mapping)
        where TVertex : unmanaged, IVertex
    {
        var attrs = new VertexAttr[mapping.Length];
        for(int i = 0; i < mapping.Length; i++) {
            var f = TVertex.Fields.GetField(mapping[i].Semantics);
            attrs[i] = new VertexAttr
            {
                Format = f.Format,
                Offset = f.Offset,
                ShaderLocation = mapping[i].Location,
            };
        }
        return new VertexBufferLayout
        {
            ArrayStride = TVertex.VertexSize,
            StepMode = VertexStepMode.Vertex,
            Attributes = attrs,
        };
    }
}

[Flags]
public enum ColorWrites : u32
{
    [EnumMapTo(Wgpu.ColorWrites.RED)] Red = 1 << 0,
    [EnumMapTo(Wgpu.ColorWrites.GREEN)] Green = 1 << 1,
    [EnumMapTo(Wgpu.ColorWrites.BLUE)] Blue = 1 << 2,
    [EnumMapTo(Wgpu.ColorWrites.ALPHA)] Alpha = 1 << 3,
    [EnumMapTo(Wgpu.ColorWrites.COLOR)] Color = Red | Green | Blue,
    [EnumMapTo(Wgpu.ColorWrites.ALL)] All = Red | Green | Blue | Alpha,
}

public readonly struct BlendState
{
    public required BlendComponent Color { get; init; }
    public required BlendComponent Alpha { get; init; }

    [SetsRequiredMembers]
    public BlendState(BlendComponent blend)
    {
        Color = blend;
        Alpha = blend;
    }

    [SetsRequiredMembers]
    public BlendState(BlendComponent colorBlend, BlendComponent alphaBlend)
    {
        Color = colorBlend;
        Alpha = alphaBlend;
    }

    public static BlendState Replace => new(BlendComponent.Replace);

    internal Wgpu.BlendState ToNative()
    {
        return new Wgpu.BlendState
        {
            color = Color.ToNative(),
            alpha = Alpha.ToNative(),
        };
    }
}

public readonly struct BlendComponent
{
    public required BlendFactor SrcFactor { get; init; }
    public required BlendFactor DstFactor { get; init; }
    public required BlendOperation Operation { get; init; }

    public static BlendComponent Replace => new()
    {
        SrcFactor = BlendFactor.One,
        DstFactor = BlendFactor.Zero,
        Operation = BlendOperation.Add,
    };

    internal Wgpu.BlendComponent ToNative()
    {
        return new()
        {
            src_factor = SrcFactor.MapOrThrow(),
            dst_factor = DstFactor.MapOrThrow(),
            operation = Operation.MapOrThrow(),
        };
    }
}

public enum BlendFactor
{
    [EnumMapTo(Wgpu.BlendFactor.Zero)] Zero = 0,
    [EnumMapTo(Wgpu.BlendFactor.One)] One = 1,
    [EnumMapTo(Wgpu.BlendFactor.Src)] Src = 2,
    [EnumMapTo(Wgpu.BlendFactor.OneMinusSrc)] OneMinusSrc = 3,
    [EnumMapTo(Wgpu.BlendFactor.SrcAlpha)] SrcAlpha = 4,
    [EnumMapTo(Wgpu.BlendFactor.OneMinusSrcAlpha)] OneMinusSrcAlpha = 5,
    [EnumMapTo(Wgpu.BlendFactor.Dst)] Dst = 6,
    [EnumMapTo(Wgpu.BlendFactor.OneMinusDst)] OneMinusDst = 7,
    [EnumMapTo(Wgpu.BlendFactor.DstAlpha)] DstAlpha = 8,
    [EnumMapTo(Wgpu.BlendFactor.OneMinusDstAlpha)] OneMinusDstAlpha = 9,
    [EnumMapTo(Wgpu.BlendFactor.SrcAlphaSaturated)] SrcAlphaSaturated = 10,
    [EnumMapTo(Wgpu.BlendFactor.Constant)] Constant = 11,
    [EnumMapTo(Wgpu.BlendFactor.OneMinusConstant)] OneMinusConstant = 12,
}

public enum BlendOperation
{
    [EnumMapTo(Wgpu.BlendOperation.Add)] Add = 0,
    [EnumMapTo(Wgpu.BlendOperation.Subtract)] Subtract = 1,
    [EnumMapTo(Wgpu.BlendOperation.ReverseSubtract)] ReverseSubtract = 2,
    [EnumMapTo(Wgpu.BlendOperation.Min)] Min = 3,
    [EnumMapTo(Wgpu.BlendOperation.Max)] Max = 4,
}

public readonly struct VertexAttr
{
    public required VertexFormat Format { get; init; }
    public required u64 Offset { get; init; }
    public required u32 ShaderLocation { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Wgpu.VertexAttribute ToNative()
    {
        return new Wgpu.VertexAttribute
        {
            format = Format.MapOrThrow(),
            offset = Offset,
            shader_location = ShaderLocation,
        };
    }
}

public enum VertexStepMode
{
    [EnumMapTo(Wgpu.VertexStepMode.Vertex)] Vertex = 0,
    [EnumMapTo(Wgpu.VertexStepMode.Instance)] Instance = 1,
}

public enum VertexFormat
{
    [EnumMapTo(Wgpu.VertexFormat.Uint8x2)] Uint8x2 = 0,
    [EnumMapTo(Wgpu.VertexFormat.Uint8x4)] Uint8x4 = 1,
    [EnumMapTo(Wgpu.VertexFormat.Sint8x2)] Sint8x2 = 2,
    [EnumMapTo(Wgpu.VertexFormat.Sint8x4)] Sint8x4 = 3,
    [EnumMapTo(Wgpu.VertexFormat.Unorm8x2)] Unorm8x2 = 4,
    [EnumMapTo(Wgpu.VertexFormat.Unorm8x4)] Unorm8x4 = 5,
    [EnumMapTo(Wgpu.VertexFormat.Snorm8x2)] Snorm8x2 = 6,
    [EnumMapTo(Wgpu.VertexFormat.Snorm8x4)] Snorm8x4 = 7,
    [EnumMapTo(Wgpu.VertexFormat.Uint16x2)] Uint16x2 = 8,
    [EnumMapTo(Wgpu.VertexFormat.Uint16x4)] Uint16x4 = 9,
    [EnumMapTo(Wgpu.VertexFormat.Sint16x2)] Sint16x2 = 10,
    [EnumMapTo(Wgpu.VertexFormat.Sint16x4)] Sint16x4 = 11,
    [EnumMapTo(Wgpu.VertexFormat.Unorm16x2)] Unorm16x2 = 12,
    [EnumMapTo(Wgpu.VertexFormat.Unorm16x4)] Unorm16x4 = 13,
    [EnumMapTo(Wgpu.VertexFormat.Snorm16x2)] Snorm16x2 = 14,
    [EnumMapTo(Wgpu.VertexFormat.Snorm16x4)] Snorm16x4 = 15,
    [EnumMapTo(Wgpu.VertexFormat.Float16x2)] Float16x2 = 16,
    [EnumMapTo(Wgpu.VertexFormat.Float16x4)] Float16x4 = 17,
    [EnumMapTo(Wgpu.VertexFormat.Float32)] Float32 = 18,
    [EnumMapTo(Wgpu.VertexFormat.Float32x2)] Float32x2 = 19,
    [EnumMapTo(Wgpu.VertexFormat.Float32x3)] Float32x3 = 20,
    [EnumMapTo(Wgpu.VertexFormat.Float32x4)] Float32x4 = 21,
    [EnumMapTo(Wgpu.VertexFormat.Uint32)] Uint32 = 22,
    [EnumMapTo(Wgpu.VertexFormat.Uint32x2)] Uint32x2 = 23,
    [EnumMapTo(Wgpu.VertexFormat.Uint32x3)] Uint32x3 = 24,
    [EnumMapTo(Wgpu.VertexFormat.Uint32x4)] Uint32x4 = 25,
    [EnumMapTo(Wgpu.VertexFormat.Sint32)] Sint32 = 26,
    [EnumMapTo(Wgpu.VertexFormat.Sint32x2)] Sint32x2 = 27,
    [EnumMapTo(Wgpu.VertexFormat.Sint32x3)] Sint32x3 = 28,
    [EnumMapTo(Wgpu.VertexFormat.Sint32x4)] Sint32x4 = 29,
    [EnumMapTo(Wgpu.VertexFormat.Float64)] Float64 = 30,
    [EnumMapTo(Wgpu.VertexFormat.Float64x2)] Float64x2 = 31,
    [EnumMapTo(Wgpu.VertexFormat.Float64x3)] Float64x3 = 32,
    [EnumMapTo(Wgpu.VertexFormat.Float64x4)] Float64x4 = 33,
}
