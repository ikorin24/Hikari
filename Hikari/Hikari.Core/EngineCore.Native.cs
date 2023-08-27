#nullable enable
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Hikari.NativeBind;

[assembly: DisableRuntimeMarshalling]

namespace Hikari;

static unsafe partial class EngineCore
{
    private const string DllDir = "native/x86_64-windows/";
    private const string CoreDll = $"{DllDir}corehikari";

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_engine_start(
        CE.EngineCoreConfig* engine_config,
        CE.HostScreenConfig* screen_config);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_create_screen(CE.HostScreenConfig* config);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_screen_resize_surface(
        Rust.Ref<CE.HostScreen> screen,
        u32 width,
        u32 height);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_screen_request_redraw(
        Rust.Ref<CE.HostScreen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.CommandEncoder> hikari_create_command_encoder(
        Rust.Ref<CE.HostScreen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void hikari_finish_command_encoder(
        Rust.Ref<CE.HostScreen> screen,
        Rust.Box<Wgpu.CommandEncoder> encoder);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiValueResult<Rust.OptionBox<Wgpu.SurfaceTexture>> hikari_get_surface_texture(
        Rust.Ref<CE.HostScreen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void hikari_destroy_surface_texture(
        Rust.Box<Wgpu.SurfaceTexture> surface_texture);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial Rust.Ref<Wgpu.Texture> hikari_surface_texture_to_texture(
        Rust.Ref<Wgpu.SurfaceTexture> surface_texture);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void hikari_present_surface_texture(
        Rust.Box<Wgpu.SurfaceTexture> surface_texture);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_screen_set_title(
        Rust.Ref<CE.HostScreen> screen,
        CE.Slice<u8> title);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.RenderPass> hikari_create_render_pass(
        Rust.MutRef<Wgpu.CommandEncoder> command_encoder,
        CE.RenderPassDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void hikari_destroy_render_pass(
        Rust.Box<Wgpu.RenderPass> render_pass);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.ComputePass> hikari_create_compute_pass(
        Rust.MutRef<Wgpu.CommandEncoder> command_encoder);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void hikari_destroy_compute_pass(
        Rust.Box<Wgpu.ComputePass> compute_pass);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_screen_set_inner_size(
        Rust.Ref<CE.HostScreen> screen,
        u32 width,
        u32 height);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiValueResult<CE.SizeU32> hikari_screen_get_inner_size(
        Rust.Ref<CE.HostScreen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_screen_set_location(
        Rust.Ref<CE.HostScreen> screen,
        i32 x,
        i32 y,
        CE.Opt<CE.MonitorId> monitor_id);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiValueResult<Vector2i> hikari_screen_get_location(
        Rust.Ref<CE.HostScreen> screen,
        CE.Opt<CE.MonitorId> monitor_id);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiValueResult<CE.Opt<CE.MonitorId>> hikari_current_monitor(
        Rust.Ref<CE.HostScreen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiValueResult<usize> hikari_monitor_count(
        Rust.Ref<CE.HostScreen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiValueResult<usize> hikari_monitors(
        Rust.Ref<CE.HostScreen> screen,
        CE.MonitorId* buf_out,
        usize buflen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_write_texture(
        Rust.Ref<CE.HostScreen> screen,
        CE.ImageCopyTexture* texture,
        CE.Slice<u8> data,
        Wgpu.ImageDataLayout* data_layout,
        Wgpu.Extent3d* size);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.BindGroupLayout> hikari_create_bind_group_layout(
        Rust.Ref<CE.HostScreen> screen,
        CE.BindGroupLayoutDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void hikari_destroy_bind_group_layout(
        Rust.Box<Wgpu.BindGroupLayout> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.BindGroup> hikari_create_bind_group(
        Rust.Ref<CE.HostScreen> screen,
        CE.BindGroupDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void hikari_destroy_bind_group(
        Rust.Box<Wgpu.BindGroup> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.PipelineLayout> hikari_create_pipeline_layout(
        Rust.Ref<CE.HostScreen> screen,
        CE.PipelineLayoutDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void hikari_destroy_pipeline_layout(
        Rust.Box<Wgpu.PipelineLayout> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.RenderPipeline> hikari_create_render_pipeline(
        Rust.Ref<CE.HostScreen> screen,
        CE.RenderPipelineDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void hikari_destroy_render_pipeline(
        Rust.Box<Wgpu.RenderPipeline> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.ComputePipeline> hikari_create_compute_pipeline(
        Rust.Ref<CE.HostScreen> screen,
        in CE.ComputePipelineDescriptor desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void hikari_destroy_compute_pipeline(
        Rust.Box<Wgpu.ComputePipeline> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.Buffer> hikari_create_buffer(
        Rust.Ref<CE.HostScreen> screen,
        u64 size,
        Wgpu.BufferUsages usage);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.Buffer> hikari_create_buffer_init(
        Rust.Ref<CE.HostScreen> screen,
        CE.Slice<u8> contents,
        Wgpu.BufferUsages usage);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void hikari_destroy_buffer(
        Rust.Box<Wgpu.Buffer> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_copy_texture_to_buffer(
        Rust.Ref<CE.HostScreen> screen,
        in CE.ImageCopyTexture source,
        in Wgpu.Extent3d copy_size,
        Rust.Ref<Wgpu.Buffer> buffer,
        in Wgpu.ImageDataLayout image_layout);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_read_buffer(
        Rust.Ref<CE.HostScreen> screen,
        CE.BufferSlice buffer_slice,
        usize token,
        delegate* unmanaged[Cdecl]<usize, ApiResult, u8*, usize, void> callback);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.Sampler> hikari_create_sampler(
        Rust.Ref<CE.HostScreen> screen,
        CE.SamplerDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void hikari_destroy_sampler(
        Rust.Box<Wgpu.Sampler> sampler);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.ShaderModule> hikari_create_shader_module(
        Rust.Ref<CE.HostScreen> screen,
        CE.Slice<u8> shader_source);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void hikari_destroy_shader_module(
        Rust.Box<Wgpu.ShaderModule> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.Texture> hikari_create_texture(
        Rust.Ref<CE.HostScreen> screen,
        CE.TextureDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.Texture> hikari_create_texture_with_data(
        Rust.Ref<CE.HostScreen> screen,
        CE.TextureDescriptor* desc,
        CE.Slice<u8> data);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void hikari_destroy_texture(
        Rust.Box<Wgpu.Texture> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_texture_format_info(
        CE.TextureFormat format,
        ref CE.TextureFormatInfo info_out);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.TextureView> hikari_create_texture_view(
        Rust.Ref<Wgpu.Texture> texture,
        CE.TextureViewDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void hikari_destroy_texture_view(
        Rust.Box<Wgpu.TextureView> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_write_buffer(
        Rust.Ref<CE.HostScreen> screen,
        Rust.Ref<Wgpu.Buffer> buffer,
        u64 offset,
        CE.Slice<u8> data);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_compute_set_pipeline(
        Rust.MutRef<Wgpu.ComputePass> pass,
        Rust.Ref<Wgpu.ComputePipeline> pipeline);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_compute_set_bind_group(
        Rust.MutRef<Wgpu.ComputePass> pass,
        u32 index,
        Rust.Ref<Wgpu.BindGroup> bind_group);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_compute_dispatch_workgroups(
        Rust.MutRef<Wgpu.ComputePass> pass,
        u32 x,
        u32 y,
        u32 z);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_set_pipeline(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        Rust.Ref<Wgpu.RenderPipeline> render_pipeline);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_set_bind_group(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        u32 index,
        Rust.Ref<Wgpu.BindGroup> bind_group);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_set_vertex_buffer(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        u32 slot,
        CE.BufferSlice buffer_slice);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_set_index_buffer(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        CE.BufferSlice buffer_slice,
        Wgpu.IndexFormat index_format);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_set_viewport(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        f32 x,
        f32 y,
        f32 w,
        f32 h,
        f32 minDepth,
        f32 maxDepth);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_draw(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        CE.RangeU32 vertices,
        CE.RangeU32 instances);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_draw_indexed(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        CE.RangeU32 indices,
        i32 base_vertex,
        CE.RangeU32 instances);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_set_ime_allowed(
        Rust.Ref<CE.HostScreen> screen,
        [MarshalAs(UnmanagedType.U1)] bool allowed);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult hikari_set_ime_position(
        Rust.Ref<CE.HostScreen> screen,
        u32 x,
        u32 y);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial usize hikari_get_tls_last_error_len();

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void hikari_take_tls_last_error(ref u8 buf);
}
