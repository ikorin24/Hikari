#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Hikari.NativeBind;
using System.Text;

namespace Hikari;

internal unsafe static partial class EngineCore
{
    private static EngineCoreConfig _config;
    private static int _isStarted = 0;

    public static bool IsStarted => _isStarted == 1;

    public static void EngineStart(in EngineCoreConfig config, in ScreenConfig screenConfig)
    {
        if(Interlocked.CompareExchange(ref _isStarted, 1, 0) == 1) {
            throw new InvalidOperationException("The engine is already running.");
        }
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            if(Thread.CurrentThread.GetApartmentState() != ApartmentState.STA) {
                throw new InvalidOperationException("The thread should be STA. (for C#, mark main method as [STAThread] attribute.)");
            }
        }

        _config = config;
        var engineConfigNative = new CH.EngineCoreConfig
        {
            on_screen_init = new(&OnScreenInit),
            on_unhandled_error = new(&OnUnhandledError),
            event_cleared = new(&EventCleared),
            event_redraw_requested = new(&EventRedrawRequested),
            event_resized = new(&EventResized),
            event_keyboard = new(&EventKeyboard),
            event_char_received = new(&EventCharReceived),
            event_mouse_button = new(&EventMouseButton),
            event_ime = new(&EventIme),
            event_wheel = new(&EventWheel),
            event_cursor_moved = new(&EventCursorMoved),
            event_cursor_entered_left = new(&EventCursorEnteredLeft),
            event_closing = new(&EventClosing),
            event_closed = new(&EventClosed),
        };

        var screenConfigNative = screenConfig.ToCoreType();

        hikari_engine_start(&engineConfigNative, &screenConfigNative).Validate();
        return;


        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static CH.ScreenId OnScreenInit(
            void* screen_,  // Rust.Box<CH.HostScreen> screen
            CH.HostScreenInfo* info
            )
        {
            // UnmanagedCallersOnly methods cannot have generic type args.
            Rust.Box<CH.HostScreen> screen = *(Rust.Box<CH.HostScreen>*)(&screen_);

            return _config.OnStart(screen, *info);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void OnUnhandledError(byte* message, nuint len)
        {
            try {
                var str = Encoding.UTF8.GetString(message, (int)len);
                Console.Error.WriteLine(str);
#if DEBUG
                System.Diagnostics.Debug.WriteLine(str);
                System.Diagnostics.Debugger.Break();
                Environment.Exit(-1);
#endif
            }
            catch {
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EventCleared(CH.ScreenId id)
        {
            _config.OnCleared(id);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static bool EventRedrawRequested(CH.ScreenId id)
        {
            return _config.OnRedrawRequested(id);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EventResized(CH.ScreenId id, u32 width, u32 height)
        {
            _config.OnResized(id, width, height);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EventKeyboard(CH.ScreenId id, Winit.VirtualKeyCode key, bool pressed)
        {
            _config.OnKeyboardInput(id, key, pressed);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EventCharReceived(CH.ScreenId id, Rune input)
        {
            _config.OnCharReceived(id, input);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EventMouseButton(CH.ScreenId id, CH.MouseButton button, bool pressed)
        {
            _config.OnMouseButton(id, button, pressed);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EventIme(CH.ScreenId id, CH.ImeInputData* input)
        {
            _config.OnImeInput(id, in *input);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EventWheel(CH.ScreenId id, f32 x_delta, f32 y_delta)
        {
            _config.OnWheel(id, x_delta, y_delta);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EventCursorMoved(CH.ScreenId id, f32 x, f32 y)
        {
            _config.OnCursorMoved(id, x, y);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EventCursorEnteredLeft(CH.ScreenId id, bool entered)
        {
            _config.OnCursorEnteredLeft(id, entered);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EventClosing(CH.ScreenId id, bool* mut_cancel)
        {
            ref bool cancel = ref *mut_cancel;
            _config.OnClosing(id, ref cancel);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static NativePointer EventClosed(CH.ScreenId id)
        {
            return _config.OnClosed(id).AsPtr();
        }
    }

    public static void CreateScreen(in ScreenConfig config)
    {
        var screenConfig = config.ToCoreType();
        hikari_create_screen(&screenConfig).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ScreenResizeSurface(this Rust.Ref<CH.HostScreen> screen, u32 width, u32 height)
    {
        hikari_screen_resize_surface(screen, width, height).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ScreenRequestRedraw(this Rust.Ref<CH.HostScreen> screen)
    {
        hikari_screen_request_redraw(screen).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Box<Wgpu.CommandEncoder> CreateCommandEncoder(this Rust.Ref<CH.HostScreen> screen)
    {
        return hikari_create_command_encoder(screen).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FinishCommandEncoder(this Rust.Ref<CH.HostScreen> screen, Rust.Box<Wgpu.CommandEncoder> encoder)
    {
        hikari_finish_command_encoder(screen, encoder);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.OptionBox<Wgpu.SurfaceTexture> GetSurfaceTexture(
        this Rust.Ref<CH.HostScreen> screen)
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
    public unsafe static void ScreenSetTitle(this Rust.Ref<CH.HostScreen> screen, ReadOnlySpan<byte> title)
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
    [DebuggerHidden]
    public static Vector2u ScreenGetInnerSize(
        this Rust.Ref<CH.HostScreen> screen)
    {
        var size = hikari_screen_get_inner_size(screen).Validate();
        return new Vector2u(size.width, size.height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void ScreenSetLocation(
        this Rust.Ref<CH.HostScreen> screen,
        i32 x,
        i32 y,
        MonitorId? monitorId)
    {
        var id = monitorId.HasValue ? CH.Opt<CH.MonitorId>.Some(monitorId.Value.Id) : CH.Opt<CH.MonitorId>.None;
        hikari_screen_set_location(screen, x, y, id).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Vector2i ScreenGetLocation(
        this Rust.Ref<CH.HostScreen> screen,
        MonitorId? monitorId)
    {
        var id = monitorId.HasValue ? CH.Opt<CH.MonitorId>.Some(monitorId.Value.Id) : CH.Opt<CH.MonitorId>.None;
        return hikari_screen_get_location(screen, id).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static usize MonitorCount(
        this Rust.Ref<CH.HostScreen> screen)
    {
        return hikari_monitor_count(screen).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static usize Monitors(
        this Rust.Ref<CH.HostScreen> screen,
        Span<CH.MonitorId> buf)
    {
        fixed(CH.MonitorId* p = buf) {
            return hikari_monitors(screen, p, (usize)buf.Length).Validate();
        }
    }

    public static MonitorId? CurrentMonitor(this Rust.Ref<CH.HostScreen> screen)
    {
        return hikari_current_monitor(screen)
            .Validate()
            .TryGetValue(out var monitor) ? new MonitorId(monitor) : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void ScreenSetInnerSize(
        this Rust.Ref<CH.HostScreen> screen,
        u32 width,
        u32 height)
        => hikari_screen_set_inner_size(screen, width, height).Validate();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void WriteTexture(
        this Rust.Ref<CH.HostScreen> screen,
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
    [DebuggerHidden]
    public static Rust.Box<Wgpu.BindGroupLayout> CreateBindGroupLayout(
        this Rust.Ref<CH.HostScreen> screen,
        in CH.BindGroupLayoutDescriptor desc)
    {
        fixed(CH.BindGroupLayoutDescriptor* descPtr = &desc) {
            return hikari_create_bind_group_layout(screen, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroyBindGroupLayout(
        this Rust.Box<Wgpu.BindGroupLayout> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_bind_group_layout(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.BindGroup> CreateBindGroup(
        this Rust.Ref<CH.HostScreen> screen,
        in CH.BindGroupDescriptor desc)
    {
        fixed(CH.BindGroupDescriptor* descPtr = &desc) {
            return hikari_create_bind_group(screen, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroyBindGroup(
        this Rust.Box<Wgpu.BindGroup> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_bind_group(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.PipelineLayout> CreatePipelineLayout(
        this Rust.Ref<CH.HostScreen> screen,
        in CH.PipelineLayoutDescriptor desc)
    {
        fixed(CH.PipelineLayoutDescriptor* descPtr = &desc) {
            return hikari_create_pipeline_layout(screen, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroyPipelineLayout(
        this Rust.Box<Wgpu.PipelineLayout> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_pipeline_layout(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.RenderPipeline> CreateRenderPipeline(
        this Rust.Ref<CH.HostScreen> screen,
        in CH.RenderPipelineDescriptor desc)
    {
        fixed(CH.RenderPipelineDescriptor* descPtr = &desc) {
            return hikari_create_render_pipeline(screen, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroyRenderPipeline(
        this Rust.Box<Wgpu.RenderPipeline> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_render_pipeline(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.ComputePipeline> CreateComputePipeline(
        this Rust.Ref<CH.HostScreen> screen,
        in CH.ComputePipelineDescriptor desc)
    {
        return hikari_create_compute_pipeline(screen, desc).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroyComputePipeline(
        this Rust.Box<Wgpu.ComputePipeline> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_compute_pipeline(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.Buffer> CreateBuffer(
        this Rust.Ref<CH.HostScreen> screen,
        u64 size,
        Wgpu.BufferUsages usage)
        => hikari_create_buffer(screen, size, usage).Validate();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.Buffer> CreateBufferInit(
        this Rust.Ref<CH.HostScreen> screen,
        CH.Slice<u8> contents,
        Wgpu.BufferUsages usage)
        => hikari_create_buffer_init(screen, contents, usage).Validate();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroyBuffer(
        this Rust.Box<Wgpu.Buffer> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_buffer(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void CopyTextureToBuffer(
        this Rust.Ref<CH.HostScreen> screen,
        in CH.ImageCopyTexture source,
        in Wgpu.Extent3d copy_size,
        Rust.Ref<Wgpu.Buffer> buffer,
        in Wgpu.ImageDataLayout image_layout)
    {
        hikari_copy_texture_to_buffer(screen, source, copy_size, buffer, image_layout).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.Sampler> CreateSampler(
        this Rust.Ref<CH.HostScreen> screen,
        in CH.SamplerDescriptor desc)
    {
        fixed(CH.SamplerDescriptor* descPtr = &desc) {
            return hikari_create_sampler(screen, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroySampler(
        this Rust.Box<Wgpu.Sampler> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_sampler(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //[DebuggerHidden]
    public static Rust.Box<Wgpu.ShaderModule> CreateShaderModule(
        this Rust.Ref<CH.HostScreen> screen,
        ReadOnlySpan<byte> shaderSource)
    {
        fixed(byte* shaderSourcePtr = shaderSource) {
            var slice = new CH.Slice<u8>(shaderSourcePtr, shaderSource.Length);
            return hikari_create_shader_module(screen, slice).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroyShaderModule(
        this Rust.Box<Wgpu.ShaderModule> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_shader_module(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.Texture> CreateTexture(
    this Rust.Ref<CH.HostScreen> screen,
    in CH.TextureDescriptor desc)
    {
        fixed(CH.TextureDescriptor* descPtr = &desc) {
            return hikari_create_texture(screen, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.Texture> CreateTextureWithData(
        this Rust.Ref<CH.HostScreen> screen,
        in CH.TextureDescriptor desc,
        CH.Slice<u8> data)
    {
        fixed(CH.TextureDescriptor* descPtr = &desc) {
            return hikari_create_texture_with_data(screen, descPtr, data).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroyTexture(
        this Rust.Box<Wgpu.Texture> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_texture(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CH.TextureFormatInfo TextureFormatInfo(
        this CH.TextureFormat format)
    {
        CH.TextureFormatInfo info_out = default;
        hikari_texture_format_info(format, ref info_out).Validate();
        return info_out;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.TextureView> CreateTextureView(
        this Rust.Ref<Wgpu.Texture> texture,
        in CH.TextureViewDescriptor desc)
    {
        fixed(CH.TextureViewDescriptor* descPtr = &desc) {
            return hikari_create_texture_view(texture, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroyTextureView(
        this Rust.Box<Wgpu.TextureView> handle)
    {
        handle.ThrowIfInvalid();
        hikari_destroy_texture_view(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void WriteBuffer(
        this Rust.Ref<CH.HostScreen> screen,
        Rust.Ref<Wgpu.Buffer> buffer,
        u64 offset,
        CH.Slice<u8> data)
    {
        screen.ThrowIfInvalid();
        buffer.ThrowIfInvalid();
        hikari_write_buffer(screen, buffer, offset, data).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void SetPipeline(
        this Rust.MutRef<Wgpu.ComputePass> pass,
        Rust.Ref<Wgpu.ComputePipeline> pipeline)
    {
        pass.ThrowIfInvalid();
        pipeline.ThrowIfInvalid();
        hikari_compute_set_pipeline(pass, pipeline).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void SetBindGroup(
        this Rust.MutRef<Wgpu.ComputePass> pass,
        u32 index,
        Rust.Ref<Wgpu.BindGroup> bindGroup)
    {
        hikari_compute_set_bind_group(pass, index, bindGroup).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DispatchWorkgroups(
        this Rust.MutRef<Wgpu.ComputePass> pass,
        u32 x,
        u32 y,
        u32 z)
    {
        hikari_compute_dispatch_workgroups(pass, x, y, z).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void SetPipeline(
        this Rust.MutRef<Wgpu.RenderPass> render_pass,
        Rust.Ref<Wgpu.RenderPipeline> render_pipeline)
    {
        render_pipeline.ThrowIfInvalid();
        hikari_set_pipeline(render_pass, render_pipeline).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void SetBindGroup(
        this Rust.MutRef<Wgpu.RenderPass> render_pass,
        u32 index,
        Rust.Ref<Wgpu.BindGroup> bind_group)
    {
        bind_group.ThrowIfInvalid();
        hikari_set_bind_group(render_pass, index, bind_group).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void SetVertexBuffer(
        this Rust.MutRef<Wgpu.RenderPass> render_pass,
        u32 slot,
        CH.BufferSlice buffer_slice)
    {
        buffer_slice.buffer.ThrowIfInvalid();
        hikari_set_vertex_buffer(render_pass, slot, buffer_slice).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void SetIndexBuffer(
        this Rust.MutRef<Wgpu.RenderPass> render_pass,
        CH.BufferSlice buffer_slice,
        Wgpu.IndexFormat index_format)
    {
        buffer_slice.buffer.ThrowIfInvalid();
        hikari_set_index_buffer(render_pass, buffer_slice, index_format).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
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
    [DebuggerHidden]
    public static void Draw(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        CH.RangeU32 vertices,
        CH.RangeU32 instances)
    {
        hikari_draw(render_pass, vertices, instances).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
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
        this Rust.Ref<CH.HostScreen> screen,
        bool allowed)
    {
        hikari_set_ime_allowed(screen, allowed).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetImePosition(
        this Rust.Ref<CH.HostScreen> screen,
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
        [DebuggerHidden]
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
        //[DebuggerHidden]
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
        [DebuggerHidden]
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

internal readonly struct EngineCoreConfig
{
    public required Func<Rust.Box<CH.HostScreen>, CH.HostScreenInfo, CH.ScreenId> OnStart { get; init; }
    public required Func<CH.ScreenId, bool> OnRedrawRequested { get; init; }
    public required Action<CH.ScreenId> OnCleared { get; init; }

    public required Action<CH.ScreenId, u32, u32> OnResized { get; init; }

    public required Action<CH.ScreenId, Winit.VirtualKeyCode, bool> OnKeyboardInput { get; init; }
    public required Action<CH.ScreenId, Rune> OnCharReceived { get; init; }
    public required Action<CH.ScreenId, CH.MouseButton, bool> OnMouseButton { get; init; }
    public required EngineCoreImeInputAction OnImeInput { get; init; }

    public required Action<CH.ScreenId, f32, f32> OnWheel { get; init; }
    public required Action<CH.ScreenId, f32, f32> OnCursorMoved { get; init; }
    public required Action<CH.ScreenId, bool> OnCursorEnteredLeft { get; init; }

    public required EngineCoreScreenClosingAction OnClosing { get; init; }
    public required Func<CH.ScreenId, Rust.OptionBox<CH.HostScreen>> OnClosed { get; init; }
}

internal delegate void EngineCoreImeInputAction(CH.ScreenId id, in CH.ImeInputData input);

internal delegate void EngineCoreScreenClosingAction(CH.ScreenId id, ref bool cancel);


internal delegate void EngineCoreRenderAction(Rust.Ref<CH.HostScreen> screen, Rust.MutRef<Wgpu.RenderPass> renderPass);
internal delegate void EngineCoreResizedAction(Rust.Ref<CH.HostScreen> screen, uint width, uint height);
internal delegate Rust.Box<Wgpu.RenderPass> OnCommandBeginFunc(
    Rust.Ref<CH.HostScreen> screen,
    Rust.Ref<Wgpu.TextureView> surfaceTextureView,
    Rust.MutRef<Wgpu.CommandEncoder> commandEncoder,
    CreateRenderPassFunc createRenderPass);

internal delegate Rust.Box<Wgpu.RenderPass> CreateRenderPassFunc(Rust.MutRef<Wgpu.CommandEncoder> commandEncoder, in CH.RenderPassDescriptor desc);
