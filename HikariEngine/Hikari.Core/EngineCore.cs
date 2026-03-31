#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Hikari.NativeBind;
using System.Text;

namespace Hikari;

internal unsafe static partial class EngineCore
{
    public static void EngineStart(Action<Engine> state, CH.EngineCoreConfig* engineConfig)
    {
#pragma warning disable CS8500 // Use address to managed type
        hikari_engine_start(&state, engineConfig).Validate();
#pragma warning restore CS8500 // Use address to managed type
    }

    public static void CreateScreen(this Rust.Ref<CH.EngineProxy> engineProxy, u64 state, in ScreenConfig config)
    {
        var bufLen = config.GetTitleByteLength();
        var buf = stackalloc u8[bufLen];
        var screenConfig = config.ToCoreType(buf, bufLen, state);
        hikari_create_screen(engineProxy, &screenConfig).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool WindowIsMaximized(this Rust.Ref<CH.Screen> screen)
    {
        return hikari_window_is_maximized(screen).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WindowSetMaximized(this Rust.Ref<CH.Screen> screen, bool maximized)
    {
        hikari_window_set_maximized(screen, maximized).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool WindowIsMinimized(this Rust.Ref<CH.Screen> screen)
    {
        return hikari_window_is_minimized(screen).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WindowSetMinimized(this Rust.Ref<CH.Screen> screen, bool minimized)
    {
        hikari_window_set_minimized(screen, minimized).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ScreenResizeSurface(this Rust.Ref<CH.Screen> screen, u32 width, u32 height)
    {
        hikari_screen_resize_surface(screen, width, height).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ScreenRequestRedraw(this Rust.Ref<CH.Screen> screen)
    {
        hikari_screen_request_redraw(screen).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Box<Wgpu.CommandEncoder> CreateCommandEncoder(this Rust.Ref<CH.Screen> screen)
    {
        return hikari_create_command_encoder(screen).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FinishCommandEncoder(this Rust.Ref<CH.Screen> screen, Rust.Box<Wgpu.CommandEncoder> encoder)
    {
        hikari_finish_command_encoder(screen, encoder);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.OptionBox<Wgpu.SurfaceTexture> GetSurfaceTexture(
        this Rust.Ref<CH.Screen> screen)
    {
        return hikari_get_surface_texture(screen).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DestroySurfaceTexture(
        this Rust.Box<Wgpu.SurfaceTexture> surface_texture)
    {
        hikari_destroy_surface_texture(surface_texture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Ref<Wgpu.Texture> SurfaceTextureToTexture(
        this Rust.Ref<Wgpu.SurfaceTexture> surface_texture)
    {
        return hikari_surface_texture_to_texture(surface_texture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PresentSurfaceTexture(this Rust.Box<Wgpu.SurfaceTexture> surface_texture)
    {
        hikari_present_surface_texture(surface_texture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void ScreenSetTitle(this Rust.Ref<CH.Screen> screen, ReadOnlySpan<byte> title)
    {
        fixed(byte* p = title) {
            var titleRaw = new CH.Slice<byte>(p, title.Length);
            hikari_screen_set_title(screen, titleRaw).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Box<Wgpu.RenderPass> CreateRenderPass(this Rust.MutRef<Wgpu.CommandEncoder> commandEncoder, in CH.RenderPassDescriptor desc)
    {
        fixed(CH.RenderPassDescriptor* descPtr = &desc) {
            return hikari_create_render_pass(commandEncoder, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DestroyRenderPass(this Rust.Box<Wgpu.RenderPass> renderPass)
    {
        hikari_destroy_render_pass(renderPass);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Box<Wgpu.ComputePass> CreateComputePass(this Rust.MutRef<Wgpu.CommandEncoder> commandEncoder)
    {
        return hikari_create_compute_pass(commandEncoder).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DestroyComputePass(this Rust.Box<Wgpu.ComputePass> computePass)
    {
        hikari_destroy_compute_pass(computePass);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2u ScreenGetInnerSize(
        this Rust.Ref<CH.Screen> screen)
    {
        var size = hikari_screen_get_inner_size(screen).Validate();
        return new Vector2u(size.width, size.height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static f64 ScreenGetScaleFactor(
        this Rust.Ref<CH.Screen> screen)
    {
        return hikari_screen_get_scale_factor(screen).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ScreenSetLocation(
        this Rust.Ref<CH.Screen> screen,
        i32 x,
        i32 y,
        MonitorId? monitorId)
    {
        var id = CH.Opt.From(monitorId?.Id);
        hikari_screen_set_location(screen, x, y, id).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i ScreenGetLocation(
        this Rust.Ref<CH.Screen> screen,
        MonitorId? monitorId)
    {
        var id = CH.Opt.From(monitorId?.Id);
        return hikari_screen_get_location(screen, id).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static usize MonitorCount(
        this Rust.Ref<CH.Screen> screen)
    {
        return hikari_monitor_count(screen).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static usize Monitors(
        this Rust.Ref<CH.Screen> screen,
        Span<CH.MonitorId> buf)
    {
        fixed(CH.MonitorId* p = buf) {
            return hikari_monitors(screen, p, (usize)buf.Length).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MonitorId? CurrentMonitor(this Rust.Ref<CH.Screen> screen)
    {
        return hikari_current_monitor(screen)
            .Validate()
            .TryGetValue(out var monitor) ? new MonitorId(monitor) : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ScreenSetInnerSize(
        this Rust.Ref<CH.Screen> screen,
        u32 width,
        u32 height)
        => hikari_screen_set_inner_size(screen, width, height).Validate();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteTexture(
        this Rust.Ref<CH.Screen> screen,
        in CH.ImageCopyTexture texture,
        CH.Slice<u8> data,
        in Wgpu.ImageDataLayout dataLayout,
        in Wgpu.Extent3d size)
    {
        fixed(CH.ImageCopyTexture* texturePtr = &texture)
        fixed(Wgpu.ImageDataLayout* dataLayoutPtr = &dataLayout)
        fixed(Wgpu.Extent3d* sizePtr = &size) {
            hikari_write_texture(screen, texturePtr, data, dataLayoutPtr, sizePtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Box<Wgpu.BindGroupLayout> CreateBindGroupLayout(
        this Rust.Ref<CH.Screen> screen,
        in CH.BindGroupLayoutDescriptor desc)
    {
        fixed(CH.BindGroupLayoutDescriptor* descPtr = &desc) {
            return hikari_create_bind_group_layout(screen, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DestroyBindGroupLayout(
        this Rust.Box<Wgpu.BindGroupLayout> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_bind_group_layout(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Box<Wgpu.BindGroup> CreateBindGroup(
        this Rust.Ref<CH.Screen> screen,
        in CH.BindGroupDescriptor desc)
    {
        fixed(CH.BindGroupDescriptor* descPtr = &desc) {
            return hikari_create_bind_group(screen, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DestroyBindGroup(
        this Rust.Box<Wgpu.BindGroup> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_bind_group(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Box<Wgpu.PipelineLayout> CreatePipelineLayout(
        this Rust.Ref<CH.Screen> screen,
        in CH.PipelineLayoutDescriptor desc)
    {
        fixed(CH.PipelineLayoutDescriptor* descPtr = &desc) {
            return hikari_create_pipeline_layout(screen, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DestroyPipelineLayout(
        this Rust.Box<Wgpu.PipelineLayout> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_pipeline_layout(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Box<Wgpu.RenderPipeline> CreateRenderPipeline(
        this Rust.Ref<CH.Screen> screen,
        in CH.RenderPipelineDescriptor desc)
    {
        fixed(CH.RenderPipelineDescriptor* descPtr = &desc) {
            return hikari_create_render_pipeline(screen, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DestroyRenderPipeline(
        this Rust.Box<Wgpu.RenderPipeline> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_render_pipeline(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Box<Wgpu.ComputePipeline> CreateComputePipeline(
        this Rust.Ref<CH.Screen> screen,
        in CH.ComputePipelineDescriptor desc)
    {
        return hikari_create_compute_pipeline(screen, desc).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DestroyComputePipeline(
        this Rust.Box<Wgpu.ComputePipeline> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_compute_pipeline(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Box<Wgpu.Buffer> CreateBuffer(
        this Rust.Ref<CH.Screen> screen,
        u64 size,
        Wgpu.BufferUsages usage)
        => hikari_create_buffer(screen, size, usage).Validate();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Box<Wgpu.Buffer> CreateBufferInit(
        this Rust.Ref<CH.Screen> screen,
        CH.Slice<u8> contents,
        Wgpu.BufferUsages usage)
        => hikari_create_buffer_init(screen, contents, usage).Validate();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DestroyBuffer(
        this Rust.Box<Wgpu.Buffer> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_buffer(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTextureToBuffer(
        this Rust.Ref<CH.Screen> screen,
        in CH.ImageCopyTexture source,
        in Wgpu.Extent3d copy_size,
        Rust.Ref<Wgpu.Buffer> buffer,
        in Wgpu.ImageDataLayout image_layout)
    {
        hikari_copy_texture_to_buffer(screen, source, copy_size, buffer, image_layout).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Box<Wgpu.Sampler> CreateSampler(
        this Rust.Ref<CH.Screen> screen,
        in CH.SamplerDescriptor desc)
    {
        fixed(CH.SamplerDescriptor* descPtr = &desc) {
            return hikari_create_sampler(screen, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DestroySampler(
        this Rust.Box<Wgpu.Sampler> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_sampler(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Box<Wgpu.ShaderModule> CreateShaderModule(
        this Rust.Ref<CH.Screen> screen,
        ReadOnlySpan<byte> shaderSource)
    {
        fixed(byte* shaderSourcePtr = shaderSource) {
            var slice = new CH.Slice<u8>(shaderSourcePtr, shaderSource.Length);
            return hikari_create_shader_module(screen, slice).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DestroyShaderModule(
        this Rust.Box<Wgpu.ShaderModule> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_shader_module(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Box<Wgpu.Texture> CreateTexture(
    this Rust.Ref<CH.Screen> screen,
    in CH.TextureDescriptor desc)
    {
        fixed(CH.TextureDescriptor* descPtr = &desc) {
            return hikari_create_texture(screen, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetTextureDescriptor(
        this Rust.Ref<Wgpu.Texture> texture,
        out CH.TextureDescriptor desc)
    {
        hikari_get_texture_descriptor(texture, out desc).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Box<Wgpu.Texture> CreateTextureWithData(
        this Rust.Ref<CH.Screen> screen,
        in CH.TextureDescriptor desc,
        CH.Slice<u8> data)
    {
        fixed(CH.TextureDescriptor* descPtr = &desc) {
            return hikari_create_texture_with_data(screen, descPtr, data).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DestroyTexture(
        this Rust.Box<Wgpu.Texture> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_texture(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Wgpu.Features TextureFormatRequiredFeatures(
        this CH.TextureFormat format)
    {
        return hikari_texture_format_required_features(format).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CH.Opt<CH.TextureSampleType> TextureFormatSampleType(
        this CH.TextureFormat format,
        CH.Opt<CH.TextureAspect> aspect)
    {
        return hikari_texture_format_sample_type(format, aspect).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CH.TupleU32U32 TextureFormatBlockDimensions(
        this CH.TextureFormat format)
    {
        return hikari_texture_format_block_dimensions(format).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CH.Opt<u32> TextureFormatBlockSize(
        this CH.TextureFormat format,
        CH.Opt<CH.TextureAspect> aspect)
    {
        return hikari_texture_format_block_size(format, aspect).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static u8 TextureFormatComponents(
        this CH.TextureFormat format,
        CH.TextureAspect aspect)
    {
        return hikari_texture_format_components(format, aspect).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TextureFormatIsSrgb(
        this CH.TextureFormat format)
    {
        return hikari_texture_format_is_srgb(format).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CH.TextureFormatFeatures TextureFormatGuaranteedFormatFeatures(
        this CH.TextureFormat format,
        Rust.Ref<CH.Screen> screen)
    {
        return hikari_texture_format_guaranteed_format_features(screen, format).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Box<Wgpu.TextureView> CreateTextureView(
        this Rust.Ref<Wgpu.Texture> texture,
        in CH.TextureViewDescriptor desc)
    {
        fixed(CH.TextureViewDescriptor* descPtr = &desc) {
            return hikari_create_texture_view(texture, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DestroyTextureView(
        this Rust.Box<Wgpu.TextureView> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_texture_view(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBuffer(
        this Rust.Ref<CH.Screen> screen,
        Rust.Ref<Wgpu.Buffer> buffer,
        u64 offset,
        CH.Slice<u8> data)
    {
        screen.ThrowIfInvalid();
        buffer.ThrowIfInvalid();
        hikari_write_buffer(screen, buffer, offset, data).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetPipeline(
        this Rust.MutRef<Wgpu.ComputePass> pass,
        Rust.Ref<Wgpu.ComputePipeline> pipeline)
    {
        pass.ThrowIfInvalid();
        pipeline.ThrowIfInvalid();
        hikari_compute_set_pipeline(pass, pipeline).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBindGroup(
        this Rust.MutRef<Wgpu.ComputePass> pass,
        u32 index,
        Rust.Ref<Wgpu.BindGroup> bindGroup)
    {
        hikari_compute_set_bind_group(pass, index, bindGroup).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DispatchWorkgroups(
        this Rust.MutRef<Wgpu.ComputePass> pass,
        u32 x,
        u32 y,
        u32 z)
    {
        hikari_compute_dispatch_workgroups(pass, x, y, z).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetPipeline(
        this Rust.MutRef<Wgpu.RenderPass> render_pass,
        Rust.Ref<Wgpu.RenderPipeline> render_pipeline)
    {
        render_pipeline.ThrowIfInvalid();
        hikari_set_pipeline(render_pass, render_pipeline).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBindGroup(
        this Rust.MutRef<Wgpu.RenderPass> render_pass,
        u32 index,
        Rust.Ref<Wgpu.BindGroup> bind_group)
    {
        bind_group.ThrowIfInvalid();
        hikari_set_bind_group(render_pass, index, bind_group).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetVertexBuffer(
        this Rust.MutRef<Wgpu.RenderPass> render_pass,
        u32 slot,
        CH.BufferSlice buffer_slice)
    {
        buffer_slice.buffer.ThrowIfInvalid();
        hikari_set_vertex_buffer(render_pass, slot, buffer_slice).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetIndexBuffer(
        this Rust.MutRef<Wgpu.RenderPass> render_pass,
        CH.BufferSlice buffer_slice,
        Wgpu.IndexFormat index_format)
    {
        buffer_slice.buffer.ThrowIfInvalid();
        hikari_set_index_buffer(render_pass, buffer_slice, index_format).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetViewport(
        this Rust.MutRef<Wgpu.RenderPass> render_pass,
        f32 x,
        f32 y,
        f32 w,
        f32 h,
        f32 minDepth,
        f32 maxDepth)
    {
        hikari_set_viewport(render_pass, x, y, w, h, minDepth, maxDepth).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Draw(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        CH.RangeU32 vertices,
        CH.RangeU32 instances)
    {
        hikari_draw(render_pass, vertices, instances).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawIndexed(
        this Rust.MutRef<Wgpu.RenderPass> render_pass,
        CH.RangeU32 indices,
        i32 base_vertex,
        CH.RangeU32 instances)
    {
        hikari_draw_indexed(render_pass, indices, base_vertex, instances).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetImeAllowed(
        this Rust.Ref<CH.Screen> screen,
        bool allowed)
    {
        hikari_set_ime_allowed(screen, allowed).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetImePosition(
        this Rust.Ref<CH.Screen> screen,
        u32 x,
        u32 y)
    {
        hikari_set_ime_position(screen, x, y).Validate();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static EngineCoreException GetTlsLastError()
    {
        var len = hikari_get_tls_last_error_len();
        if(len == 0) {
            return new EngineCoreException(null);
        }
        var pool = System.Buffers.ArrayPool<byte>.Shared;
        var buf = pool.Rent((int)len);
        try {
            hikari_take_tls_last_error(ref MemoryMarshal.GetArrayDataReference(buf));
            var message = Encoding.UTF8.GetString(buf.AsSpan(0, (int)len));
            return new EngineCoreException(message);
        }
        finally {
            pool.Return(buf);
        }
    }

#pragma warning disable 0649    // field never assigned

    private readonly struct ApiResult
    {
        private readonly bool _success;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Validate()
        {
            if(_success == false) {
                throw GetTlsLastError();
            }
        }
    }

    private readonly struct ApiBoxResult<T> where T : INativeTypeNonReprC
    {
        private readonly bool _success;
        private readonly void* _nativePtr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Rust.Box<T> Validate()
        {
            if(_success == false) {
                throw GetTlsLastError();
            }
            var nativePtr = _nativePtr;
            Debug.Assert(nativePtr != null);
            return *(Rust.Box<T>*)(&nativePtr);
        }
    }

    private readonly struct ApiValueResult<T> where T : unmanaged
    {
        private readonly bool _success;
        private readonly T _value;

        [UnscopedRef]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Validate()
        {
            if(_success == false) {
                throw GetTlsLastError();
            }
            return ref _value;
        }
    }
#pragma warning restore 0649    // field never assigned
}

//internal readonly struct EngineCoreConfig
//{
//    public required Func<Rust.Box<CH.Screen>, CH.ScreenInfo, CH.ScreenId> OnScreenInit { get; init; }
//    public required Func<CH.ScreenId, bool> OnRedrawRequested { get; init; }
//    public required Action<CH.ScreenId> OnCleared { get; init; }

//    public required Action<CH.ScreenId, u32, u32> OnResized { get; init; }

//    public required Action<CH.ScreenId, CH.KeyCode, bool> OnKeyboardInput { get; init; }
//    public required Action<CH.ScreenId, Rune> OnCharReceived { get; init; }
//    public required Action<CH.ScreenId, CH.MouseButton, bool> OnMouseButton { get; init; }
//    public required EngineCoreImeInputAction OnImeInput { get; init; }

//    public required Action<CH.ScreenId, f32, f32> OnWheel { get; init; }
//    public required Action<CH.ScreenId, f32, f32> OnCursorMoved { get; init; }
//    public required Action<CH.ScreenId, bool> OnCursorEnteredLeft { get; init; }

//    public required EngineCoreScreenClosingAction OnClosing { get; init; }
//    public required Func<CH.ScreenId, Rust.OptionBox<CH.Screen>> OnClosed { get; init; }
//}

//internal delegate void EngineCoreImeInputAction(CH.ScreenId id, in CH.ImeInputData input);

//internal delegate void EngineCoreScreenClosingAction(CH.ScreenId id, ref bool cancel);


//internal delegate void EngineCoreRenderAction(Rust.Ref<CH.Screen> screen, Rust.MutRef<Wgpu.RenderPass> renderPass);
//internal delegate void EngineCoreResizedAction(Rust.Ref<CH.Screen> screen, uint width, uint height);
//internal delegate Rust.Box<Wgpu.RenderPass> OnCommandBeginFunc(
//    Rust.Ref<CH.Screen> screen,
//    Rust.Ref<Wgpu.TextureView> surfaceTextureView,
//    Rust.MutRef<Wgpu.CommandEncoder> commandEncoder,
//    CreateRenderPassFunc createRenderPass);

//internal delegate Rust.Box<Wgpu.RenderPass> CreateRenderPassFunc(Rust.MutRef<Wgpu.CommandEncoder> commandEncoder, in CH.RenderPassDescriptor desc);
