#nullable enable
using u8 = System.Byte;
using u16 = System.UInt16;
using u32 = System.UInt32;
using u64 = System.UInt64;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WgpuSample;

static unsafe partial class EngineCore
{
	private const string EngineCoreDll = "wgpu_sample";

	[LibraryImport(EngineCoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static partial void elffy_engine_start(
		delegate* unmanaged[Cdecl]<HostScreenHandle, HostScreenCallbacks> init);

	[LibraryImport(EngineCoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial RenderPipelineHandle elffy_add_render_pipeline(
		HostScreenHandle screen,
		in RenderPipelineInfo render_pipeline);

	[LibraryImport(EngineCoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial BufferHandle elffy_create_buffer_init(
		HostScreenHandle screen,
		Sliceffi<u8> contents,
		BufferUsages usage);

	[LibraryImport(EngineCoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial void elffy_set_pipeline(
		RenderPassHandle render_pass,
		RenderPipelineHandle render_pipeline);

	[LibraryImport(EngineCoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial void elffy_draw_buffer(
		RenderPassHandle render_pass,
		in SlotBufferSliceffi vertex_buffer,
		in RangeU32ffi vertices_range,
		in RangeU32ffi instances_range);

	[LibraryImport(EngineCoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial void elffy_draw_buffer_indexed(
		RenderPassHandle render_pass,
		in SlotBufferSliceffi vertex_buffer,
		in IndexBufferSliceffi index_buffer,
		in RangeU32ffi indices_range,
		in RangeU32ffi instances_range);

	[LibraryImport(EngineCoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial void elffy_draw_buffers_indexed(
		RenderPassHandle render_pass,
		Sliceffi<SlotBufferSliceffi> vertex_buffers,
		in IndexBufferSliceffi index_buffer,
		in RangeU32ffi indices_range,
		in RangeU32ffi instances_range);

}