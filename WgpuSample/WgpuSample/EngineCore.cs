#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using u8 = System.Byte;
using u16 = System.UInt16;
using u32 = System.UInt32;
using u64 = System.UInt64;

[assembly: DisableRuntimeMarshalling]

namespace WgpuSample
{
    internal unsafe static partial class EngineCore
    {
        private static Action<HostScreenHandle>? _init;
        private static Action<HostScreenHandle, RenderPassHandle>? _onRender;

        [DoesNotReturn]
        public static Never EngineStart(Action<HostScreenHandle> init, Action<HostScreenHandle, RenderPassHandle> onRender)
        {
            _init = init;
            _onRender = onRender;
            elffy_engine_start(&OnInit);
            throw new UnreachableException();

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static HostScreenCallbacks OnInit(HostScreenHandle screen)
            {
                _init?.Invoke(screen);

                return new HostScreenCallbacks
                {
                    on_render = &OnRender,
                };
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static void OnRender(HostScreenHandle screen, RenderPassHandle render_pass)
            {
                _onRender?.Invoke(screen, render_pass);
            }
        }
    }

    static unsafe partial class EngineCore
    {
        private const string EngineCoreDll = "wgpu_sample";

        [LibraryImport(EngineCoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static partial void elffy_engine_start(delegate* unmanaged[Cdecl]<HostScreenHandle, HostScreenCallbacks> init);

        [LibraryImport(EngineCoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial RenderPipelineHandle elffy_add_render_pipeline(HostScreenHandle screen, in RenderPipelineInfo render_pipeline);

        [LibraryImport(EngineCoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial BufferHandle elffy_create_buffer_init(HostScreenHandle screen, Sliceffi<u8> contents, BufferUsages usage);

        [LibraryImport(EngineCoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void elffy_set_pipeline(RenderPassHandle render_pass, RenderPipelineHandle render_pipeline);

        [LibraryImport(EngineCoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void elffy_draw_buffer(
            RenderPassHandle render_pass,
            in SlotBufferSliceffi vertex_buffer,
            in RangeU32ffi vertices_range,
            in RangeU32ffi instances_range
            );

        [LibraryImport(EngineCoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void elffy_draw_buffer_indexed(
            RenderPassHandle render_pass,
            in SlotBufferSliceffi vertex_buffer,
            in IndexBufferSliceffi index_buffer,
            in RangeU32ffi indices_range,
            in RangeU32ffi instances_range
            );

        [LibraryImport(EngineCoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void elffy_draw_buffers_indexed(
            RenderPassHandle render_pass,
            Sliceffi<SlotBufferSliceffi> vertex_buffers,
            in IndexBufferSliceffi index_buffer,
            in RangeU32ffi indices_range,
            in RangeU32ffi instances_range
            );

    }



    public sealed class NativeApiException : Exception
    {
        public NativeApiException()
        {
        }

        public NativeApiException(string message) : base(message)
        {
        }
    }

    public sealed class Never
    {
        private Never() => throw new UnreachableException();
    }
}
