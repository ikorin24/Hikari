#nullable enable
using Hikari.NativeBind;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Hikari;

public readonly ref struct ComputePass
{
    private readonly Screen _screen;
    private readonly Rust.Box<Wgpu.ComputePass> _native;
    private readonly Rust.Box<Wgpu.CommandEncoder> _encoder;

    private ComputePass(Screen screen, Rust.Box<Wgpu.ComputePass> native, Rust.Box<Wgpu.CommandEncoder> encoder)
    {
        _screen = screen;
        _native = native;
        _encoder = encoder;
    }

    private static readonly ReleaseComputePass _release = static self =>
    {
        self._native.DestroyComputePass();
        self._screen.AsRefChecked().FinishCommandEncoder(self._encoder);
    };

    internal static OwnComputePass Create(Screen screen)
    {
        var encoder = screen.AsRefChecked().CreateCommandEncoder();
        var native = encoder.AsMut().CreateComputePass();
        return new OwnComputePass(new(screen, native, encoder), _release);
    }

    public void SetPipeline(ComputePipeline pipeline)
    {
        _native.AsMut().SetPipeline(pipeline.NativeRef);
    }

    public void SetBindGroup(u32 index, BindGroup bindGroup)
    {
        _native.AsMut().SetBindGroup(index, bindGroup.NativeRef);
    }

    public void DispatchWorkgroups(u32 x, u32 y, u32 z)
    {
        _native.AsMut().DispatchWorkgroups(x, y, z);
    }
}

public readonly ref struct OwnComputePass
{
    private readonly ComputePass _value;
    private readonly ReleaseComputePass? _release;

    [MemberNotNullWhen(false, nameof(_value))]
    [MemberNotNullWhen(false, nameof(_release))]
    public bool IsNone => _release == null;

    public static OwnComputePass None => default;

    [Obsolete("Don't use default constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public OwnComputePass() => throw new NotSupportedException("Don't use default constructor.");

    internal OwnComputePass(ComputePass value, ReleaseComputePass release)
    {
        ArgumentNullException.ThrowIfNull(release);
        _value = value;
        _release = release;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComputePass AsValue()
    {
        if(IsNone) {
            ThrowNoValue();
        }
        return _value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComputePass AsValue(out OwnComputePass self)
    {
        self = this;
        return AsValue();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAsValue(out ComputePass value)
    {
        value = _value;
        return !IsNone;
    }

    public void Dispose()
    {
        if(IsNone) { return; }
        _release.Invoke(_value);
    }

    [DoesNotReturn]
    [DebuggerHidden]
    private static void ThrowNoValue() => throw new InvalidOperationException("no value exists");

    public static explicit operator ComputePass(OwnComputePass own) => own.AsValue();
}

internal delegate void ReleaseComputePass(ComputePass computePass);
