#nullable enable
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Hikari.NativeBind;
using bool_u8 = byte;

[assembly: DisableRuntimeMarshalling]

namespace Hikari;

static unsafe partial class EngineCore
{
    private const string CoreDll = "corehikari";

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_engine_start(
        CH.EngineCoreConfig* engine_config,
        CH.ScreenConfig* screen_config);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_create_screen(CH.ScreenConfig* config);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_screen_resize_surface(
        Rust.Ref<CH.Screen> screen,
        u32 width,
        u32 height);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_screen_request_redraw(
        Rust.Ref<CH.Screen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiBoxResult<Wgpu.CommandEncoder> hikari_create_command_encoder(
        Rust.Ref<CH.Screen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void hikari_finish_command_encoder(
        Rust.Ref<CH.Screen> screen,
        Rust.Box<Wgpu.CommandEncoder> encoder);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiValueResult<Rust.OptionBox<Wgpu.SurfaceTexture>> hikari_get_surface_texture(
        Rust.Ref<CH.Screen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void hikari_destroy_surface_texture(
        Rust.Box<Wgpu.SurfaceTexture> surface_texture);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial Rust.Ref<Wgpu.Texture> hikari_surface_texture_to_texture(
        Rust.Ref<Wgpu.SurfaceTexture> surface_texture);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void hikari_present_surface_texture(
        Rust.Box<Wgpu.SurfaceTexture> surface_texture);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_screen_set_title(
        Rust.Ref<CH.Screen> screen,
        CH.Slice<u8> title);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiBoxResult<Wgpu.RenderPass> hikari_create_render_pass(
        Rust.MutRef<Wgpu.CommandEncoder> command_encoder,
        CH.RenderPassDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void hikari_destroy_render_pass(
        Rust.Box<Wgpu.RenderPass> render_pass);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiBoxResult<Wgpu.ComputePass> hikari_create_compute_pass(
        Rust.MutRef<Wgpu.CommandEncoder> command_encoder);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void hikari_destroy_compute_pass(
        Rust.Box<Wgpu.ComputePass> compute_pass);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_screen_set_inner_size(
        Rust.Ref<CH.Screen> screen,
        u32 width,
        u32 height);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiValueResult<CH.SizeU32> hikari_screen_get_inner_size(
        Rust.Ref<CH.Screen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiValueResult<f64> hikari_screen_get_scale_factor(
        Rust.Ref<CH.Screen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_screen_set_location(
        Rust.Ref<CH.Screen> screen,
        i32 x,
        i32 y,
        CH.Opt<CH.MonitorId> monitor_id);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiValueResult<Vector2i> hikari_screen_get_location(
        Rust.Ref<CH.Screen> screen,
        CH.Opt<CH.MonitorId> monitor_id);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiValueResult<CH.Opt<CH.MonitorId>> hikari_current_monitor(
        Rust.Ref<CH.Screen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiValueResult<usize> hikari_monitor_count(
        Rust.Ref<CH.Screen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiValueResult<usize> hikari_monitors(
        Rust.Ref<CH.Screen> screen,
        CH.MonitorId* buf_out,
        usize buflen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_write_texture(
        Rust.Ref<CH.Screen> screen,
        CH.ImageCopyTexture* texture,
        CH.Slice<u8> data,
        Wgpu.ImageDataLayout* data_layout,
        Wgpu.Extent3d* size);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiBoxResult<Wgpu.BindGroupLayout> hikari_create_bind_group_layout(
        Rust.Ref<CH.Screen> screen,
        CH.BindGroupLayoutDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void hikari_destroy_bind_group_layout(
        Rust.Box<Wgpu.BindGroupLayout> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiBoxResult<Wgpu.BindGroup> hikari_create_bind_group(
        Rust.Ref<CH.Screen> screen,
        CH.BindGroupDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void hikari_destroy_bind_group(
        Rust.Box<Wgpu.BindGroup> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiBoxResult<Wgpu.PipelineLayout> hikari_create_pipeline_layout(
        Rust.Ref<CH.Screen> screen,
        CH.PipelineLayoutDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void hikari_destroy_pipeline_layout(
        Rust.Box<Wgpu.PipelineLayout> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiBoxResult<Wgpu.RenderPipeline> hikari_create_render_pipeline(
        Rust.Ref<CH.Screen> screen,
        CH.RenderPipelineDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void hikari_destroy_render_pipeline(
        Rust.Box<Wgpu.RenderPipeline> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiBoxResult<Wgpu.ComputePipeline> hikari_create_compute_pipeline(
        Rust.Ref<CH.Screen> screen,
        in CH.ComputePipelineDescriptor desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void hikari_destroy_compute_pipeline(
        Rust.Box<Wgpu.ComputePipeline> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiBoxResult<Wgpu.Buffer> hikari_create_buffer(
        Rust.Ref<CH.Screen> screen,
        u64 size,
        Wgpu.BufferUsages usage);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiBoxResult<Wgpu.Buffer> hikari_create_buffer_init(
        Rust.Ref<CH.Screen> screen,
        CH.Slice<u8> contents,
        Wgpu.BufferUsages usage);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void hikari_destroy_buffer(
        Rust.Box<Wgpu.Buffer> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_copy_texture_to_buffer(
        Rust.Ref<CH.Screen> screen,
        in CH.ImageCopyTexture source,
        in Wgpu.Extent3d copy_size,
        Rust.Ref<Wgpu.Buffer> buffer,
        in Wgpu.ImageDataLayout image_layout);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_read_buffer(
        Rust.Ref<CH.Screen> screen,
        CH.BufferSlice buffer_slice,
        usize token,
        delegate* unmanaged[Cdecl]<usize, ApiResult, u8*, usize, void> callback);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiBoxResult<Wgpu.Sampler> hikari_create_sampler(
        Rust.Ref<CH.Screen> screen,
        CH.SamplerDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void hikari_destroy_sampler(
        Rust.Box<Wgpu.Sampler> sampler);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiBoxResult<Wgpu.ShaderModule> hikari_create_shader_module(
        Rust.Ref<CH.Screen> screen,
        CH.Slice<u8> shader_source);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void hikari_destroy_shader_module(
        Rust.Box<Wgpu.ShaderModule> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiBoxResult<Wgpu.Texture> hikari_create_texture(
        Rust.Ref<CH.Screen> screen,
        CH.TextureDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_get_texture_descriptor(
        Rust.Ref<Wgpu.Texture> texture,
        out CH.TextureDescriptor desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiBoxResult<Wgpu.Texture> hikari_create_texture_with_data(
        Rust.Ref<CH.Screen> screen,
        CH.TextureDescriptor* desc,
        CH.Slice<u8> data);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void hikari_destroy_texture(
        Rust.Box<Wgpu.Texture> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiValueResult<Wgpu.Features> hikari_texture_format_required_features(
        CH.TextureFormat format);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiValueResult<CH.Opt<CH.TextureSampleType>> hikari_texture_format_sample_type(
        CH.TextureFormat format,
        CH.Opt<CH.TextureAspect> aspect);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiValueResult<CH.TupleU32U32> hikari_texture_format_block_dimensions(
        CH.TextureFormat format);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiValueResult<CH.Opt<u32>> hikari_texture_format_block_size(
        CH.TextureFormat format,
        CH.Opt<CH.TextureAspect> aspect);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiValueResult<u8> hikari_texture_format_components(
        CH.TextureFormat format,
        CH.TextureAspect aspect);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiValueResult<bool_u8> hikari_texture_format_is_srgb(
        CH.TextureFormat format);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiValueResult<CH.TextureFormatFeatures> hikari_texture_format_guaranteed_format_features(
        Rust.Ref<CH.Screen> screen,
        CH.TextureFormat format);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiBoxResult<Wgpu.TextureView> hikari_create_texture_view(
        Rust.Ref<Wgpu.Texture> texture,
        CH.TextureViewDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void hikari_destroy_texture_view(
        Rust.Box<Wgpu.TextureView> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_write_buffer(
        Rust.Ref<CH.Screen> screen,
        Rust.Ref<Wgpu.Buffer> buffer,
        u64 offset,
        CH.Slice<u8> data);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_compute_set_pipeline(
        Rust.MutRef<Wgpu.ComputePass> pass,
        Rust.Ref<Wgpu.ComputePipeline> pipeline);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_compute_set_bind_group(
        Rust.MutRef<Wgpu.ComputePass> pass,
        u32 index,
        Rust.Ref<Wgpu.BindGroup> bind_group);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_compute_dispatch_workgroups(
        Rust.MutRef<Wgpu.ComputePass> pass,
        u32 x,
        u32 y,
        u32 z);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_set_pipeline(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        Rust.Ref<Wgpu.RenderPipeline> render_pipeline);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_set_bind_group(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        u32 index,
        Rust.Ref<Wgpu.BindGroup> bind_group);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_set_vertex_buffer(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        u32 slot,
        CH.BufferSlice buffer_slice);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_set_index_buffer(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        CH.BufferSlice buffer_slice,
        Wgpu.IndexFormat index_format);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_set_viewport(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        f32 x,
        f32 y,
        f32 w,
        f32 h,
        f32 minDepth,
        f32 maxDepth);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_draw(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        CH.RangeU32 vertices,
        CH.RangeU32 instances);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_draw_indexed(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        CH.RangeU32 indices,
        i32 base_vertex,
        CH.RangeU32 instances);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_set_ime_allowed(
        Rust.Ref<CH.Screen> screen,
        [MarshalAs(UnmanagedType.U1)] bool allowed);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ApiResult hikari_set_ime_position(
        Rust.Ref<CH.Screen> screen,
        u32 x,
        u32 y);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial usize hikari_get_tls_last_error_len();

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void hikari_take_tls_last_error(ref u8 buf);
}
