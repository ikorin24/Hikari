#nullable enable
using NonZeroUsize = System.UIntPtr;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Elffy.NativeBind;

[assembly: DisableRuntimeMarshalling]

namespace Elffy;

static unsafe partial class EngineCore
{
    private const string DllDir = "native/x86_64-windows/";
    private const string CoreDll = $"{DllDir}coreelffy";

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_engine_start(
        CE.EngineCoreConfig* engine_config,
        CE.HostScreenConfig* screen_config);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_screen_resize_surface(
        Rust.Ref<CE.HostScreen> screen,
        u32 width,
        u32 height);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_screen_request_close(
        Rust.Ref<CE.HostScreen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_screen_request_redraw(
        Rust.Ref<CE.HostScreen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiValueResult<CE.BeginCommandData> elffy_screen_begin_command(
        Rust.Ref<CE.HostScreen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_screen_finish_command(
        Rust.Ref<CE.HostScreen> screen,
        Rust.Box<Wgpu.CommandEncoder> command_encoder,
        Rust.Box<Wgpu.SurfaceTexture> surface_tex,
        Rust.Box<Wgpu.TextureView> surface_tex_view);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_screen_set_title(
        Rust.Ref<CE.HostScreen> screen,
        CE.Slice<u8> title);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.RenderPass> elffy_create_render_pass(
        Rust.MutRef<Wgpu.CommandEncoder> command_encoder,
        CE.RenderPassDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_render_pass(
        Rust.Box<Wgpu.RenderPass> render_pass);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_screen_set_inner_size(
        Rust.Ref<CE.HostScreen> screen,
        u32 width,
        u32 height);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiValueResult<CE.SizeU32> elffy_screen_get_inner_size(
        Rust.Ref<CE.HostScreen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_write_texture(
        Rust.Ref<CE.HostScreen> screen,
        CE.ImageCopyTexture* texture,
        CE.Slice<u8> data,
        Wgpu.ImageDataLayout* data_layout,
        Wgpu.Extent3d* size);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.BindGroupLayout> elffy_create_bind_group_layout(
        Rust.Ref<CE.HostScreen> screen,
        CE.BindGroupLayoutDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_bind_group_layout(
        Rust.Box<Wgpu.BindGroupLayout> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.BindGroup> elffy_create_bind_group(
        Rust.Ref<CE.HostScreen> screen,
        CE.BindGroupDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_bind_group(
        Rust.Box<Wgpu.BindGroup> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.PipelineLayout> elffy_create_pipeline_layout(
        Rust.Ref<CE.HostScreen> screen,
        CE.PipelineLayoutDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_pipeline_layout(
        Rust.Box<Wgpu.PipelineLayout> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.RenderPipeline> elffy_create_render_pipeline(
        Rust.Ref<CE.HostScreen> screen,
        CE.RenderPipelineDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_render_pipeline(
        Rust.Box<Wgpu.RenderPipeline> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.Buffer> elffy_create_buffer_init(
        Rust.Ref<CE.HostScreen> screen,
        CE.Slice<u8> contents,
        Wgpu.BufferUsages usage);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_buffer(
        Rust.Box<Wgpu.Buffer> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.Sampler> elffy_create_sampler(
        Rust.Ref<CE.HostScreen> screen,
        CE.SamplerDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_sampler(
        Rust.Box<Wgpu.Sampler> sampler);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.ShaderModule> elffy_create_shader_module(
        Rust.Ref<CE.HostScreen> screen,
        CE.Slice<u8> shader_source);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_shader_module(
        Rust.Box<Wgpu.ShaderModule> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.Texture> elffy_create_texture(
        Rust.Ref<CE.HostScreen> screen,
        CE.TextureDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.Texture> elffy_create_texture_with_data(
        Rust.Ref<CE.HostScreen> screen,
        CE.TextureDescriptor* desc,
        CE.Slice<u8> data);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_texture(
        Rust.Box<Wgpu.Texture> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.TextureView> elffy_create_texture_view(
        Rust.Ref<Wgpu.Texture> texture,
        CE.TextureViewDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_texture_view(
        Rust.Box<Wgpu.TextureView> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_write_buffer(
        Rust.Ref<CE.HostScreen> screen,
        Rust.Ref<Wgpu.Buffer> buffer,
        u64 offset,
        CE.Slice<u8> data);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_set_pipeline(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        Rust.Ref<Wgpu.RenderPipeline> render_pipeline);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_set_bind_group(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        u32 index,
        Rust.Ref<Wgpu.BindGroup> bind_group);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_set_vertex_buffer(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        u32 slot,
        CE.BufferSlice buffer_slice);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_set_index_buffer(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        CE.BufferSlice buffer_slice,
        Wgpu.IndexFormat index_format);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_draw(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        CE.RangeU32 vertices,
        CE.RangeU32 instances);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_draw_indexed(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        CE.RangeU32 indices,
        i32 base_vertex,
        CE.RangeU32 instances);
}
