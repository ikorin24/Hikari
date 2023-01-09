#nullable enable
using u8 = System.Byte;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using WgpuSample.Bind;

namespace WgpuSample;

static unsafe partial class EngineCore
{
    private const string DllDir = "native/x86_64-windows/";
    private const string CoreDll = $"{DllDir}elffycore";

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void elffy_engine_start(
        EngineCoreConfig* engine_config,
        HostScreenConfig* screen_config);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial BindGroupLayoutHandle elffy_create_bind_group_layout(
        HostScreenHandle screen,
        BindGroupLayoutDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial byte elffy_destroy_bind_group_layout(
        HostScreenHandle screen,
        BindGroupLayoutHandle handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial BindGroupHandle elffy_create_bind_group(
        HostScreenHandle screen,
        BindGroupDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial byte elffy_destroy_bind_group(
        HostScreenHandle screen,
        BindGroupHandle handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial PipelineLayoutHandle elffy_create_pipeline_layout(
        HostScreenHandle screen,
        PipelineLayoutDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial byte elffy_destroy_pipeline_layout(
        HostScreenHandle screen,
        PipelineLayoutHandle handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial RenderPipelineHandle elffy_create_render_pipeline(
        HostScreenHandle screen,
        RenderPipelineDescription* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial byte elffy_destroy_render_pipeline(
        HostScreenHandle screen,
        RenderPipelineHandle handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial BufferHandle elffy_create_buffer_init(
        HostScreenHandle screen,
        Slice<u8> contents,
        wgpu_BufferUsages usage);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial byte elffy_destroy_buffer(
        HostScreenHandle screen,
        BufferHandle handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SamplerHandle elffy_create_sampler(
        HostScreenHandle screen,
        SamplerDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial byte elffy_destroy_sampler(
        HostScreenHandle screen,
        SamplerHandle sampler);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial ShaderModuleHandle elffy_create_shader_module(
        HostScreenHandle screen,
        Slice<u8> shader_source);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial byte elffy_destroy_shader_module(
        HostScreenHandle screen,
        ShaderModuleHandle handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial TextureHandle elffy_create_texture(
        HostScreenHandle screen,
        TextureDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial TextureHandle elffy_create_texture_with_data(
        HostScreenHandle screen,
        TextureDescriptor* desc,
        Slice<u8> data);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial byte elffy_destroy_texture(
        HostScreenHandle screen,
        TextureHandle handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial TextureViewHandle elffy_create_texture_view(
        HostScreenHandle screen,
        TextureHandle texture,
        TextureViewDescriptor* desc);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial byte elffy_destroy_texture_view(
        HostScreenHandle screen,
        TextureViewHandle handle);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void elffy_set_pipeline(
        RenderPassHandle render_pass,
        RenderPipelineHandle render_pipeline);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void elffy_draw_buffer(
        RenderPassHandle render_pass,
        DrawBufferArg* arg);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void elffy_draw_buffer_indexed(
        RenderPassHandle render_pass,
        DrawBufferIndexedArg* arg);

    [LibraryImport(CoreDll), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void elffy_draw_buffers_indexed(
        RenderPassHandle render_pass,
        DrawBuffersIndexedArg* arg);
}
