#nullable enable
using u8 = System.Byte;
using u32 = System.UInt32;
using i32 = System.Int32;
using u64 = System.UInt64;
using NonZeroUsize = System.UIntPtr;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Elffy.Bind;

[assembly: DisableRuntimeMarshalling]

namespace Elffy;

static unsafe partial class EngineCore
{
    private const string DllDir = "native/x86_64-windows/";
    private const string CoreDll = $"{DllDir}elffycore";

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial NonZeroUsize elffy_engine_start(
        Elffycore.EngineCoreConfig* engine_config,
        Elffycore.HostScreenConfig* screen_config);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_screen_resize_surface(
        Ref<Elffycore.HostScreen> screen,
        u32 width,
        u32 height);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_screen_request_redraw(
        Ref<Elffycore.HostScreen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiValueResult<Elffycore.BeginCommandData> elffy_screen_begin_command(
        Ref<Elffycore.HostScreen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_screen_finish_command(
        Ref<Elffycore.HostScreen> screen,
        Box<Wgpu.CommandEncoder> command_encoder,
        Box<Wgpu.SurfaceTexture> surface_tex,
        Box<Wgpu.TextureView> surface_tex_view);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_screen_set_title(
        Ref<Elffycore.HostScreen> screen,
        Slice<u8> title);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.RenderPass> elffy_create_render_pass(
        MutRef<Wgpu.CommandEncoder> command_encoder,
        RenderPassDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_render_pass(
        Box<Wgpu.RenderPass> render_pass);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_screen_set_inner_size(
        Ref<Elffycore.HostScreen> screen,
        u32 width,
        u32 height);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiValueResult<Elffycore.SizeU32> elffy_screen_get_inner_size(
        Ref<Elffycore.HostScreen> screen);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_write_texture(
        Ref<Elffycore.HostScreen> screen,
        ImageCopyTexture* texture,
        Slice<u8> data,
        wgpu_ImageDataLayout* data_layout,
        wgpu_Extent3d* size);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.BindGroupLayout> elffy_create_bind_group_layout(
        Ref<Elffycore.HostScreen> screen,
        BindGroupLayoutDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_bind_group_layout(
        Box<Wgpu.BindGroupLayout> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.BindGroup> elffy_create_bind_group(
        Ref<Elffycore.HostScreen> screen,
        BindGroupDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_bind_group(
        Box<Wgpu.BindGroup> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.PipelineLayout> elffy_create_pipeline_layout(
        Ref<Elffycore.HostScreen> screen,
        PipelineLayoutDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_pipeline_layout(
        Box<Wgpu.PipelineLayout> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.RenderPipeline> elffy_create_render_pipeline(
        Ref<Elffycore.HostScreen> screen,
        RenderPipelineDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_render_pipeline(
        Box<Wgpu.RenderPipeline> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.Buffer> elffy_create_buffer_init(
        Ref<Elffycore.HostScreen> screen,
        Slice<u8> contents,
        wgpu_BufferUsages usage);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_buffer(
        Box<Wgpu.Buffer> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.Sampler> elffy_create_sampler(
        Ref<Elffycore.HostScreen> screen,
        SamplerDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_sampler(
        Box<Wgpu.Sampler> sampler);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.ShaderModule> elffy_create_shader_module(
        Ref<Elffycore.HostScreen> screen,
        Slice<u8> shader_source);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_shader_module(
        Box<Wgpu.ShaderModule> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.Texture> elffy_create_texture(
        Ref<Elffycore.HostScreen> screen,
        TextureDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.Texture> elffy_create_texture_with_data(
        Ref<Elffycore.HostScreen> screen,
        TextureDescriptor* desc,
        Slice<u8> data);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_texture(
        Box<Wgpu.Texture> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiBoxResult<Wgpu.TextureView> elffy_create_texture_view(
        Ref<Wgpu.Texture> texture,
        TextureViewDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_texture_view(
        Box<Wgpu.TextureView> handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_write_buffer(
        Ref<Elffycore.HostScreen> screen,
        Ref<Wgpu.Buffer> buffer,
        u64 offset,
        Slice<u8> data);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_set_pipeline(
        MutRef<Wgpu.RenderPass> render_pass,
        Ref<Wgpu.RenderPipeline> render_pipeline);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_set_bind_group(
        MutRef<Wgpu.RenderPass> render_pass,
        u32 index,
        Ref<Wgpu.BindGroup> bind_group);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_set_vertex_buffer(
        MutRef<Wgpu.RenderPass> render_pass,
        u32 slot,
        BufferSlice buffer_slice);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_set_index_buffer(
        MutRef<Wgpu.RenderPass> render_pass,
        BufferSlice buffer_slice,
        wgpu_IndexFormat index_format);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_draw(
        MutRef<Wgpu.RenderPass> render_pass,
        RangeU32 vertices,
        RangeU32 instances);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_draw_indexed(
        MutRef<Wgpu.RenderPass> render_pass,
        RangeU32 indices,
        i32 base_vertex,
        RangeU32 instances);
}
