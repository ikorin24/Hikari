#nullable enable
using Elffy.Bind;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy;

public sealed class PipelineLayout : IEngineManaged, IDisposable
{
    private IHostScreen? _screen;
    private Box<Wgpu.PipelineLayout> _native;

    public IHostScreen? Screen => _screen;

    internal Ref<Wgpu.PipelineLayout> NativeRef => _native;

    private PipelineLayout(IHostScreen screen, Box<Wgpu.PipelineLayout> native)
    {
        _screen = screen;
        _native = native;
    }

    public unsafe static PipelineLayout Create(IHostScreen screen, in PipelineLayoutDescriptor desc)
    {
        var bindGroupLayouts = desc.BindGroupLayouts.Span;
        var bindGroupLayoutsNative = stackalloc Ref<Wgpu.BindGroupLayout>[bindGroupLayouts.Length];
        for(int i = 0; i < bindGroupLayouts.Length; i++) {
            bindGroupLayoutsNative[i] = bindGroupLayouts[i].NativeRef;
        }

        var descNative = new CE.PipelineLayoutDescriptor(bindGroupLayoutsNative, (nuint)bindGroupLayouts.Length);
        var pipelineLayout = screen.AsRef().CreatePipelineLayout(descNative);
        return new PipelineLayout(screen, pipelineLayout);

    }

    public void Dispose()
    {
        if(_native.IsInvalid) {
            return;
        }
        _native.DestroyPipelineLayout();
        _native = Box<Wgpu.PipelineLayout>.Invalid;
        _screen = null;
    }
}

public readonly struct PipelineLayoutDescriptor
{
    public required ReadOnlyMemory<BindGroupLayout> BindGroupLayouts { get; init; }
}
