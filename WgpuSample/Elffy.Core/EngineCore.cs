#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Elffy.NativeBind;
using System.Text;
using System.Collections.Generic;

namespace Elffy;

internal unsafe static partial class EngineCore
{
    [ThreadStatic]
    private static List<NativeError>? _nativeErrorStore;
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
        var engineConfigNative = new CE.EngineCoreConfig
        {
            err_dispatcher = new(&DispatchError),
            on_screen_init = new(&OnScreenInit),
            event_cleared = new(&EventCleared),
            event_redraw_requested = new(&EventRedrawRequested),
            event_resized = new(&EventResized),
            event_keyboard = new(&EventKeyboard),
            event_char_received = new(&EventCharReceived),
            event_ime = new(&EventIme),
            event_wheel = new(&EventWheel),
            event_cursor_moved = new(&EventCursorMoved),
            event_cursor_entered_left = new(&EventCursorEnteredLeft),
            event_closing = new(&EventClosing),
            event_closed = new(&EventClosed),
        };

        var screenConfigNative = screenConfig.ToCoreType();

        elffy_engine_start(&engineConfigNative, &screenConfigNative).Validate();
        return;


        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static CE.ScreenId OnScreenInit(
            void* screen_,  // Rust.Box<CE.HostScreen> screen
            CE.HostScreenInfo* info
            )
        {
            // UnmanagedCallersOnly methods cannot have generic type args.
            Rust.Box<CE.HostScreen> screen = *(Rust.Box<CE.HostScreen>*)(&screen_);

            return _config.OnStart(screen, *info);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EventCleared(CE.ScreenId id)
        {
            _config.OnCleared(id);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static bool EventRedrawRequested(CE.ScreenId id)
        {
            return _config.OnRedrawRequested(id);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EventResized(CE.ScreenId id, u32 width, u32 height)
        {
            _config.OnResized(id, width, height);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EventKeyboard(CE.ScreenId id, Winit.VirtualKeyCode key, bool pressed)
        {
            _config.OnKeyboardInput(id, key, pressed);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EventCharReceived(CE.ScreenId id, Rune input)
        {
            _config.OnCharReceived(id, input);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EventIme(CE.ScreenId id, CE.ImeInputData* input)
        {
            _config.OnImeInput(id, in *input);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EventWheel(CE.ScreenId id, f32 x_delta, f32 y_delta)
        {
            _config.OnWheel(id, x_delta, y_delta);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EventCursorMoved(CE.ScreenId id, f32 x, f32 y)
        {
            _config.OnCursorMoved(id, x, y);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EventCursorEnteredLeft(CE.ScreenId id, bool entered)
        {
            _config.OnCursorEnteredLeft(id, entered);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EventClosing(CE.ScreenId id, bool* mut_cancel)
        {
            ref bool cancel = ref *mut_cancel;
            _config.OnClosing(id, ref cancel);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static NativePointer EventClosed(CE.ScreenId id)
        {
            return _config.OnClosed(id).AsPtr();
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void DispatchError(CE.ErrMessageId id, u8* messagePtr, usize messageByteLen)
        {
            var len = (int)usize.Min(messageByteLen, (usize)int.MaxValue);
            var message = Encoding.UTF8.GetString(messagePtr, len);
            _nativeErrorStore ??= new();
            _nativeErrorStore.Add(new(id, message));
        }
    }

    public static void CreateScreen(in ScreenConfig config)
    {
        var screenConfig = config.ToCoreType();
        elffy_create_screen(&screenConfig).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ScreenResizeSurface(this Rust.Ref<CE.HostScreen> screen, u32 width, u32 height)
    {
        elffy_screen_resize_surface(screen, width, height).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ScreenRequestRedraw(this Rust.Ref<CE.HostScreen> screen)
    {
        elffy_screen_request_redraw(screen).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ScreenBeginCommand(
        Screen screen,
        out CommandEncoder encoder)
    {
        ref readonly var data = ref elffy_screen_begin_command(screen.AsRefChecked()).Validate();
        if(data.success) {
            encoder = new CommandEncoder(
                screen,
                data.command_encoder.Unwrap(),
                data.surface_texture.Unwrap(),
                data.surface_texture_view.Unwrap());
            return true;
        }
        else {
            encoder = CommandEncoder.Invalid;
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ScreenFinishCommand(
        CommandEncoder encoder)
    {
        var (screen, commandEncoder, surfaceTexture, surfaceTextureView) = encoder;
        elffy_screen_finish_command(screen.AsRefChecked(), commandEncoder, surfaceTexture, surfaceTextureView).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void ScreenSetTitle(this Rust.Ref<CE.HostScreen> screen, ReadOnlySpan<byte> title)
    {
        fixed(byte* p = title) {
            var titleRaw = new CE.Slice<byte>(p, title.Length);
            elffy_screen_set_title(screen, titleRaw).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Box<Wgpu.RenderPass> CreateRenderPass(this Rust.MutRef<Wgpu.CommandEncoder> commandEncoder, in CE.RenderPassDescriptor desc)
    {
        fixed(CE.RenderPassDescriptor* descPtr = &desc) {
            return elffy_create_render_pass(commandEncoder, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DestroyRenderPass(this Rust.Box<Wgpu.RenderPass> renderPass)
    {
        elffy_destroy_render_pass(renderPass);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Box<Wgpu.ComputePass> CreateComputePass(this Rust.MutRef<Wgpu.CommandEncoder> commandEncoder)
    {
        return elffy_create_compute_pass(commandEncoder).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DestroyComputePass(this Rust.Box<Wgpu.ComputePass> computePass)
    {
        elffy_destroy_compute_pass(computePass);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Vector2u ScreenGetInnerSize(
        this Rust.Ref<CE.HostScreen> screen)
    {
        var size = elffy_screen_get_inner_size(screen).Validate();
        return new Vector2u(size.width, size.height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void ScreenSetLocation(
        this Rust.Ref<CE.HostScreen> screen,
        i32 x,
        i32 y,
        MonitorId? monitorId)
    {
        var id = monitorId.HasValue ? CE.Opt<CE.MonitorId>.Some(monitorId.Value.Id) : CE.Opt<CE.MonitorId>.None;
        elffy_screen_set_location(screen, x, y, id).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Vector2i ScreenGetLocation(
        this Rust.Ref<CE.HostScreen> screen,
        MonitorId? monitorId)
    {
        var id = monitorId.HasValue ? CE.Opt<CE.MonitorId>.Some(monitorId.Value.Id) : CE.Opt<CE.MonitorId>.None;
        return elffy_screen_get_location(screen, id).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static usize MonitorCount(
        this Rust.Ref<CE.HostScreen> screen)
    {
        return elffy_monitor_count(screen).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static usize Monitors(
        this Rust.Ref<CE.HostScreen> screen,
        Span<CE.MonitorId> buf)
    {
        fixed(CE.MonitorId* p = buf) {
            return elffy_monitors(screen, p, (usize)buf.Length).Validate();
        }
    }

    public static MonitorId? CurrentMonitor(this Rust.Ref<CE.HostScreen> screen)
    {
        return elffy_current_monitor(screen)
            .Validate()
            .TryGetValue(out var monitor) ? new MonitorId(monitor) : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void ScreenSetInnerSize(
        this Rust.Ref<CE.HostScreen> screen,
        u32 width,
        u32 height)
        => elffy_screen_set_inner_size(screen, width, height).Validate();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void WriteTexture(
        this Rust.Ref<CE.HostScreen> screen,
        in CE.ImageCopyTexture texture,
        CE.Slice<u8> data,
        in Wgpu.ImageDataLayout dataLayout,
        in Wgpu.Extent3d size)
    {
        fixed(CE.ImageCopyTexture* texturePtr = &texture)
        fixed(Wgpu.ImageDataLayout* dataLayoutPtr = &dataLayout)
        fixed(Wgpu.Extent3d* sizePtr = &size) {
            elffy_write_texture(screen, texturePtr, data, dataLayoutPtr, sizePtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.BindGroupLayout> CreateBindGroupLayout(
        this Rust.Ref<CE.HostScreen> screen,
        in CE.BindGroupLayoutDescriptor desc)
    {
        fixed(CE.BindGroupLayoutDescriptor* descPtr = &desc) {
            return elffy_create_bind_group_layout(screen, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroyBindGroupLayout(
        this Rust.Box<Wgpu.BindGroupLayout> handle)
    {
        handle.ThrowIfInvalid();
        elffy_destroy_bind_group_layout(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.BindGroup> CreateBindGroup(
        this Rust.Ref<CE.HostScreen> screen,
        in CE.BindGroupDescriptor desc)
    {
        fixed(CE.BindGroupDescriptor* descPtr = &desc) {
            return elffy_create_bind_group(screen, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroyBindGroup(
        this Rust.Box<Wgpu.BindGroup> handle)
    {
        handle.ThrowIfInvalid();
        elffy_destroy_bind_group(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.PipelineLayout> CreatePipelineLayout(
        this Rust.Ref<CE.HostScreen> screen,
        in CE.PipelineLayoutDescriptor desc)
    {
        fixed(CE.PipelineLayoutDescriptor* descPtr = &desc) {
            return elffy_create_pipeline_layout(screen, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroyPipelineLayout(
        this Rust.Box<Wgpu.PipelineLayout> handle)
    {
        handle.ThrowIfInvalid();
        elffy_destroy_pipeline_layout(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.RenderPipeline> CreateRenderPipeline(
        this Rust.Ref<CE.HostScreen> screen,
        in CE.RenderPipelineDescriptor desc)
    {
        fixed(CE.RenderPipelineDescriptor* descPtr = &desc) {
            return elffy_create_render_pipeline(screen, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroyRenderPipeline(
        this Rust.Box<Wgpu.RenderPipeline> handle)
    {
        handle.ThrowIfInvalid();
        elffy_destroy_render_pipeline(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.ComputePipeline> CreateComputePipeline(
        this Rust.Ref<CE.HostScreen> screen,
        in CE.ComputePipelineDescriptor desc)
    {
        return elffy_create_compute_pipeline(screen, desc).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroyComputePipeline(
        this Rust.Box<Wgpu.ComputePipeline> handle)
    {
        handle.ThrowIfInvalid();
        elffy_destroy_compute_pipeline(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.Buffer> CreateBuffer(
        this Rust.Ref<CE.HostScreen> screen,
        u64 size,
        Wgpu.BufferUsages usage)
        => elffy_create_buffer(screen, size, usage).Validate();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.Buffer> CreateBufferInit(
        this Rust.Ref<CE.HostScreen> screen,
        CE.Slice<u8> contents,
        Wgpu.BufferUsages usage)
        => elffy_create_buffer_init(screen, contents, usage).Validate();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroyBuffer(
        this Rust.Box<Wgpu.Buffer> handle)
    {
        handle.ThrowIfInvalid();
        elffy_destroy_buffer(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.Sampler> CreateSampler(
        this Rust.Ref<CE.HostScreen> screen,
        in CE.SamplerDescriptor desc)
    {
        fixed(CE.SamplerDescriptor* descPtr = &desc) {
            return elffy_create_sampler(screen, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroySampler(
        this Rust.Box<Wgpu.Sampler> handle)
    {
        handle.ThrowIfInvalid();
        elffy_destroy_sampler(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.ShaderModule> CreateShaderModule(
        this Rust.Ref<CE.HostScreen> screen,
        ReadOnlySpan<byte> shaderSource)
    {
        fixed(byte* shaderSourcePtr = shaderSource) {
            var slice = new CE.Slice<u8>(shaderSourcePtr, shaderSource.Length);
            return elffy_create_shader_module(screen, slice).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroyShaderModule(
        this Rust.Box<Wgpu.ShaderModule> handle)
    {
        handle.ThrowIfInvalid();
        elffy_destroy_shader_module(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.Texture> CreateTexture(
    this Rust.Ref<CE.HostScreen> screen,
    in CE.TextureDescriptor desc)
    {
        fixed(CE.TextureDescriptor* descPtr = &desc) {
            return elffy_create_texture(screen, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.Texture> CreateTextureWithData(
        this Rust.Ref<CE.HostScreen> screen,
        in CE.TextureDescriptor desc,
        CE.Slice<u8> data)
    {
        fixed(CE.TextureDescriptor* descPtr = &desc) {
            return elffy_create_texture_with_data(screen, descPtr, data).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroyTexture(
        this Rust.Box<Wgpu.Texture> handle)
    {
        handle.ThrowIfInvalid();
        elffy_destroy_texture(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CE.TextureFormatInfo TextureFormatInfo(
        this CE.TextureFormat format)
    {
        CE.TextureFormatInfo info_out = default;
        elffy_texture_format_info(format, ref info_out).Validate();
        return info_out;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static Rust.Box<Wgpu.TextureView> CreateTextureView(
        this Rust.Ref<Wgpu.Texture> texture,
        in CE.TextureViewDescriptor desc)
    {
        fixed(CE.TextureViewDescriptor* descPtr = &desc) {
            return elffy_create_texture_view(texture, descPtr).Validate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DestroyTextureView(
        this Rust.Box<Wgpu.TextureView> handle)
    {
        handle.ThrowIfInvalid();
        elffy_destroy_texture_view(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void WriteBuffer(
        this Rust.Ref<CE.HostScreen> screen,
        Rust.Ref<Wgpu.Buffer> buffer,
        u64 offset,
        CE.Slice<u8> data)
    {
        screen.ThrowIfInvalid();
        buffer.ThrowIfInvalid();
        elffy_write_buffer(screen, buffer, offset, data).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void SetPipeline(
        this Rust.MutRef<Wgpu.ComputePass> pass,
        Rust.Ref<Wgpu.ComputePipeline> pipeline)
    {
        pass.ThrowIfInvalid();
        pipeline.ThrowIfInvalid();
        elffy_compute_set_pipeline(pass, pipeline).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void SetBindGroup(
        this Rust.MutRef<Wgpu.ComputePass> pass,
        u32 index,
        Rust.Ref<Wgpu.BindGroup> bindGroup)
    {
        elffy_compute_set_bind_group(pass, index, bindGroup).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DispatchWorkgroups(
        this Rust.MutRef<Wgpu.ComputePass> pass,
        u32 x,
        u32 y,
        u32 z)
    {
        elffy_compute_dispatch_workgroups(pass, x, y, z).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void SetPipeline(
        this Rust.MutRef<Wgpu.RenderPass> render_pass,
        Rust.Ref<Wgpu.RenderPipeline> render_pipeline)
    {
        render_pipeline.ThrowIfInvalid();
        elffy_set_pipeline(render_pass, render_pipeline).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void SetBindGroup(
        this Rust.MutRef<Wgpu.RenderPass> render_pass,
        u32 index,
        Rust.Ref<Wgpu.BindGroup> bind_group)
    {
        bind_group.ThrowIfInvalid();
        elffy_set_bind_group(render_pass, index, bind_group).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void SetVertexBuffer(
        this Rust.MutRef<Wgpu.RenderPass> render_pass,
        u32 slot,
        CE.BufferSlice buffer_slice)
    {
        buffer_slice.buffer.ThrowIfInvalid();
        elffy_set_vertex_buffer(render_pass, slot, buffer_slice).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void SetIndexBuffer(
        this Rust.MutRef<Wgpu.RenderPass> render_pass,
        CE.BufferSlice buffer_slice,
        Wgpu.IndexFormat index_format)
    {
        buffer_slice.buffer.ThrowIfInvalid();
        elffy_set_index_buffer(render_pass, buffer_slice, index_format).Validate();
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
        elffy_set_viewport(render_pass, x, y, w, h, minDepth, maxDepth).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void Draw(
        Rust.MutRef<Wgpu.RenderPass> render_pass,
        CE.RangeU32 vertices,
        CE.RangeU32 instances)
    {
        elffy_draw(render_pass, vertices, instances).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void DrawIndexed(
        this Rust.MutRef<Wgpu.RenderPass> render_pass,
        CE.RangeU32 indices,
        i32 base_vertex,
        CE.RangeU32 instances)
    {
        elffy_draw_indexed(render_pass, indices, base_vertex, instances).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetImeAllowed(
        this Rust.Ref<CE.HostScreen> screen,
        bool allowed)
    {
        elffy_set_ime_allowed(screen, allowed).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetImePosition(
        this Rust.Ref<CE.HostScreen> screen,
        u32 x,
        u32 y)
    {
        elffy_set_ime_position(screen, x, y).Validate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerNonUserCode]
    private static void ThrowNativeErrorIfNotZero(usize errorCount)
    {
        if(errorCount != 0) {
            ThrowNativeError(errorCount);

            [DoesNotReturn]
            [DebuggerNonUserCode]
            static void ThrowNativeError(usize errorCount)
            {
                Debug.Assert(errorCount != 0);
                var store = _nativeErrorStore;
                if(store == null) {
                    throw EngineCoreException.NewUnknownError();
                }
                else {
                    EngineCoreException exception;
                    {
                        ReadOnlySpan<NativeError> errors = CollectionsMarshal.AsSpan(store);
                        exception = new EngineCoreException(errors);
                    }
                    store.Clear();
                    throw exception;
                }
            }
        }
    }

    private readonly struct ApiResult
    {
        private readonly usize _errorCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public void Validate() => EngineCore.ThrowNativeErrorIfNotZero(_errorCount);
    }

    private readonly struct ApiBoxResult<T> where T : INativeTypeNonReprC
    {
        // (_errorCount, _nativePtr) is (0, not null) or (not 0, null)

        private readonly usize _errorCount;
        private readonly void* _nativePtr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //[DebuggerHidden]
        public Rust.Box<T> Validate()
        {
            var nativePtr = _nativePtr;
            Debug.Assert((_errorCount == 0 && nativePtr != null) || (_errorCount > 0 && nativePtr == null));
            EngineCore.ThrowNativeErrorIfNotZero(_errorCount);
            Debug.Assert(_errorCount == 0);
            Debug.Assert(nativePtr != null);
            return *(Rust.Box<T>*)(&nativePtr);
        }
    }

    private readonly struct ApiValueResult<T> where T : unmanaged
    {
        private readonly usize _errorCount;
        private readonly T _value;

        [UnscopedRef]
        public ref readonly T Validate()
        {
            EngineCore.ThrowNativeErrorIfNotZero(_errorCount);
            return ref _value;
        }
    }
}

internal readonly struct EngineCoreConfig
{
    public required Func<Rust.Box<CE.HostScreen>, CE.HostScreenInfo, CE.ScreenId> OnStart { get; init; }
    public required Func<CE.ScreenId, bool> OnRedrawRequested { get; init; }
    public required Action<CE.ScreenId> OnCleared { get; init; }

    public required Action<CE.ScreenId, u32, u32> OnResized { get; init; }

    public required Action<CE.ScreenId, Winit.VirtualKeyCode, bool> OnKeyboardInput { get; init; }
    public required Action<CE.ScreenId, Rune> OnCharReceived { get; init; }
    public required EngineCoreImeInputAction OnImeInput { get; init; }

    public required Action<CE.ScreenId, f32, f32> OnWheel { get; init; }
    public required Action<CE.ScreenId, f32, f32> OnCursorMoved { get; init; }
    public required Action<CE.ScreenId, bool> OnCursorEnteredLeft { get; init; }

    public required EngineCoreScreenClosingAction OnClosing { get; init; }
    public required Func<CE.ScreenId, Rust.OptionBox<CE.HostScreen>> OnClosed { get; init; }
}

internal delegate void EngineCoreImeInputAction(CE.ScreenId id, in CE.ImeInputData input);

internal delegate void EngineCoreScreenClosingAction(CE.ScreenId id, ref bool cancel);


internal delegate void EngineCoreRenderAction(Rust.Ref<CE.HostScreen> screen, Rust.MutRef<Wgpu.RenderPass> renderPass);
internal delegate void EngineCoreResizedAction(Rust.Ref<CE.HostScreen> screen, uint width, uint height);
internal delegate Rust.Box<Wgpu.RenderPass> OnCommandBeginFunc(
    Rust.Ref<CE.HostScreen> screen,
    Rust.Ref<Wgpu.TextureView> surfaceTextureView,
    Rust.MutRef<Wgpu.CommandEncoder> commandEncoder,
    CreateRenderPassFunc createRenderPass);

internal delegate Rust.Box<Wgpu.RenderPass> CreateRenderPassFunc(Rust.MutRef<Wgpu.CommandEncoder> commandEncoder, in CE.RenderPassDescriptor desc);
