#nullable enable
using Elffy.Bind;
using System;

namespace Elffy;

public sealed class RenderPipeline : IEngineManaged, IDisposable
{
    private IHostScreen? _screen;
    private Box<Wgpu.RenderPipeline> _native;

    public IHostScreen? Screen => _screen;

    private RenderPipeline(IHostScreen screen, Box<Wgpu.RenderPipeline> native)
    {
        _screen = screen;
        _native = native;
    }

    public static RenderPipeline Create(IHostScreen screen, PipelineLayout layout, Shader shader)
    {
        throw new NotImplementedException();

        //new CE.RenderPipelineDescriptor
        //{
        //    layout = layout.NativeRef,
        //    vertex = new()
        //    {
        //        module = shader.NativeRef,
        //        entry_point = Slice.FromFixedSpanUnsafe("vs_main"u8),
        //        buffers = Slice.FromFixedSpanUnsafe(stackalloc CE.VertexBufferLayout[]
        //            {
        //            vertexBufferLayout,
        //            instanceBufferLayout,
        //        }),
        //    },
        //    fragment = new Opt<CE.FragmentState>(new CE.FragmentState()
        //    {
        //        module = shader,
        //        entry_point = Slice.FromFixedSpanUnsafe("fs_main"u8),
        //        targets = Slice.FromFixedSpanUnsafe(stackalloc Opt<CE.ColorTargetState>[]
        //                {
        //            Opt<CE.ColorTargetState>.Some(new()
        //            {
        //                format = surfaceFormat,
        //                blend = Opt<Wgpu.BlendState>.Some(Wgpu.BlendState.REPLACE),
        //                write_mask = Wgpu.ColorWrites.ALL,
        //            })
        //        }),
        //    }),
        //    primitive = new()
        //    {
        //        topology = Wgpu.PrimitiveTopology.TriangleList,
        //        strip_index_format = Opt<Wgpu.IndexFormat>.None,
        //        front_face = Wgpu.FrontFace.Ccw,
        //        cull_mode = Opt<Wgpu.Face>.Some(Wgpu.Face.Back),
        //        polygon_mode = Wgpu.PolygonMode.Fill,
        //    },
        //    depth_stencil = Opt<CE.DepthStencilState>.Some(new()
        //    {
        //        format = depthTextureData.Format,
        //        depth_write_enabled = true,
        //        depth_compare = Wgpu.CompareFunction.Less,
        //        stencil = Wgpu.StencilState.Default,
        //        bias = Wgpu.DepthBiasState.Default,
        //    }),
        //    multisample = Wgpu.MultisampleState.Default,
        //    multiview = NonZeroU32OrNone.None,
        //};
    }

    public void Dispose()
    {
        if(_native.IsInvalid) {
            return;
        }
        _native.DestroyRenderPipeline();
        _native = Box<Wgpu.RenderPipeline>.Invalid;
        _screen = null;
    }
}
