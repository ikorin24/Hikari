#nullable enable
using u8 = System.Byte;
using u32 = System.UInt32;
using i32 = System.Int32;
using u64 = System.UInt64;
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
    private static partial void elffy_engine_start(
        EngineCoreConfig* engine_config,
        HostScreenConfig* screen_config);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_write_texture(
        HostScreenHandle screen,
        ImageCopyTexture* texture,
        Slice<u8> data,
        wgpu_ImageDataLayout* data_layout,
        wgpu_Extent3d* size);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiRefResult<BindGroupLayoutHandle> elffy_create_bind_group_layout(
        HostScreenHandle screen,
        BindGroupLayoutDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_bind_group_layout(
        BindGroupLayoutHandle handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiRefResult<BindGroupHandle> elffy_create_bind_group(
        HostScreenHandle screen,
        BindGroupDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_bind_group(
        BindGroupHandle handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiRefResult<PipelineLayoutHandle> elffy_create_pipeline_layout(
        HostScreenHandle screen,
        PipelineLayoutDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_pipeline_layout(
        PipelineLayoutHandle handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiRefResult<RenderPipelineHandle> elffy_create_render_pipeline(
        HostScreenHandle screen,
        RenderPipelineDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_render_pipeline(
        RenderPipelineHandle handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiRefResult<BufferHandle> elffy_create_buffer_init(
        HostScreenHandle screen,
        Slice<u8> contents,
        wgpu_BufferUsages usage);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_buffer(
        BufferHandle handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiRefResult<SamplerHandle> elffy_create_sampler(
        HostScreenHandle screen,
        SamplerDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_sampler(
        SamplerHandle sampler);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiRefResult<ShaderModuleHandle> elffy_create_shader_module(
        HostScreenHandle screen,
        Slice<u8> shader_source);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_shader_module(
        ShaderModuleHandle handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiRefResult<TextureHandle> elffy_create_texture(
        HostScreenHandle screen,
        TextureDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiRefResult<TextureHandle> elffy_create_texture_with_data(
        HostScreenHandle screen,
        TextureDescriptor* desc,
        Slice<u8> data);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_texture(
        TextureHandle handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiRefResult<TextureViewHandle> elffy_create_texture_view(
        HostScreenHandle screen,
        TextureHandle texture,
        TextureViewDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void elffy_destroy_texture_view(
        TextureViewHandle handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_write_buffer(
        HostScreenHandle screen,
        BufferHandle buffer,
        u64 offset,
        Slice<u8> data);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_set_pipeline(
        RenderPassRef render_pass,
        RenderPipelineHandle render_pipeline);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_set_bind_group(
        RenderPassRef render_pass,
        u32 index,
        BindGroupHandle bind_group);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_set_vertex_buffer(
        RenderPassRef render_pass,
        u32 slot,
        BufSlice buffer_slice);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_set_index_buffer(
        RenderPassRef render_pass,
        BufSlice buffer_slice,
        wgpu_IndexFormat index_format);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_draw(
        RenderPassRef render_pass,
        RangeU32 vertices,
        RangeU32 instances);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial ApiResult elffy_draw_indexed(
        RenderPassRef render_pass,
        RangeU32 indices,
        i32 base_vertex,
        RangeU32 instances);


    //[LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    //private static partial ApiResult elffy_draw_buffer(
    //    RenderPassRef render_pass,
    //    DrawBufferArg* arg);

    //[LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    //private static partial ApiResult elffy_draw_buffer_indexed(
    //    RenderPassRef render_pass,
    //    DrawBufferIndexedArg* arg);

    //[LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    //private static partial ApiResult elffy_draw_buffers_indexed(
    //    RenderPassRef render_pass,
    //    DrawBuffersIndexedArg* arg);
}
