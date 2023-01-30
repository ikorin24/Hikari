#nullable enable
using u8 = System.Byte;
using u32 = System.UInt32;
using i32 = System.Int32;
using f32 = System.Single;
using u64 = System.UInt64;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Elffy.Bind;
using System.Text;
using System.Collections.Generic;

namespace Elffy
{
    internal unsafe static partial class EngineCore
    {
        [ThreadStatic]
        private static List<NativeError>? _nativeErrorStore;
        [ThreadStatic]
        private static Action<IntPtr, nuint>? _writeBufferWriterTemp;
        private static EngineCoreConfig _config;
        private static int _isStarted = 0;

        public static bool IsStarted => _isStarted == 1;

        [DoesNotReturn]
        public static Never EngineStart(in EngineCoreConfig config, in HostScreenConfig screenConfig)
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
            var engineCoreConfigRaw = new Elffycore.EngineCoreConfig
            {
                err_dispatcher = new(&DispatchError),
                on_screen_init = new(&OnScreenInit),
                event_cleared = new(&EventCleared),
                event_redraw_requested = new(&EventRedrawRequested),
                event_resized = new(&EventResized),
            };

            var screenConfigRaw = new Elffycore.HostScreenConfig
            {
                title = Slice<u8>.Empty,
                style = screenConfig.Style,
                width = screenConfig.Width,
                height = screenConfig.Height,
                backend = screenConfig.Backend,
            };

            var errorCount = elffy_engine_start(&engineCoreConfigRaw, &screenConfigRaw);
            Debug.Assert(errorCount > 0);
            ThrowNativeErrorIfNotZero(errorCount);

            throw new UnreachableException();


            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static void OnScreenInit(
                void* screen_,  // Box<Elffycore.HostScreen> screen
                Elffycore.HostScreenInfo* info,
                Elffycore.HostScreenId id
                )
            {
                // UnmanagedCallersOnly methods cannot have generic type args.
                Box<Elffycore.HostScreen> screen = *(Box<Elffycore.HostScreen>*)(&screen_);

                _config.OnStart(screen, *info, id);
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static void EventCleared(Elffycore.HostScreenId id)
            {
                _config.OnCleared(id);
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static void EventRedrawRequested(Elffycore.HostScreenId id)
            {
                _config.OnRedrawRequested(id);
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static void EventResized(Elffycore.HostScreenId id, u32 width, u32 height)
            {
                _config.OnResized(id, width, height);
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static void DispatchError(Elffycore.ErrMessageId id, byte* messagePtr, nuint messageByteLen)
            {
                var len = (int)nuint.Min(messageByteLen, (nuint)int.MaxValue);
                var message = Encoding.UTF8.GetString(messagePtr, len);
                _nativeErrorStore ??= new();
                _nativeErrorStore.Add(new(id, message));
            }
        }

        private static readonly CreateRenderPassFunc _createRenderPassFunc =
            (MutRef<Wgpu.CommandEncoder> command_encoder, in RenderPassDescriptor desc) =>
            {
                fixed(RenderPassDescriptor* descPtr = &desc) {
                    return elffy_create_render_pass(command_encoder, descPtr).Validate();
                }
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ScreenResizeSurface(this Ref<Elffycore.HostScreen> screen, u32 width, u32 height)
        {
            elffy_screen_resize_surface(screen, width, height).Validate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ScreenRequestRedraw(this Ref<Elffycore.HostScreen> screen)
        {
            elffy_screen_request_redraw(screen).Validate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ScreenBeginCommand(
            this Ref<Elffycore.HostScreen> screen,
            out Box<Wgpu.CommandEncoder> commandEncoder,
            out Box<Wgpu.SurfaceTexture> surfaceTexture,
            out Box<Wgpu.TextureView> surfaceTextureView)
        {
            ref readonly var data = ref elffy_screen_begin_command(screen).Validate();
            commandEncoder = data.command_encoder.UnwrapUnchecked();
            surfaceTexture = data.surface_texture.UnwrapUnchecked();
            surfaceTextureView = data.surface_texture_view.UnwrapUnchecked();
            return data.success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ScreenFinishCommand(
            this Ref<Elffycore.HostScreen> screen,
            Box<Wgpu.CommandEncoder> commandEncoder,
            Box<Wgpu.SurfaceTexture> surfaceTexture,
            Box<Wgpu.TextureView> surfaceTextureView)
        {
            elffy_screen_finish_command(screen, commandEncoder, surfaceTexture, surfaceTextureView).Validate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void ScreenSetTitle(this Ref<Elffycore.HostScreen> screen, ReadOnlySpan<byte> title)
        {
            fixed(byte* p = title) {
                var titleRaw = new Slice<byte>(p, title.Length);
                elffy_screen_set_title(screen, titleRaw).Validate();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Box<Wgpu.RenderPass> CreateRenderPass(this MutRef<Wgpu.CommandEncoder> commandEncoder, in RenderPassDescriptor desc)
        {
            fixed(RenderPassDescriptor* descPtr = &desc) {
                return elffy_create_render_pass(commandEncoder, descPtr).Validate();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DestroyRenderPass(this Box<Wgpu.RenderPass> renderPass)
        {
            elffy_destroy_render_pass(renderPass);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //[DebuggerHidden]
        public static (u32 Width, u32 Height) ScreenGetInnerSize(
            Ref<Elffycore.HostScreen> screen)
        {
            var size = elffy_screen_get_inner_size(screen).Validate();
            return (size.width, size.height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void ScreenSetInnerSize(
            Ref<Elffycore.HostScreen> screen,
            u32 width,
            u32 height)
            => elffy_screen_set_inner_size(screen, width, height).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void WriteTexture(
            Ref<Elffycore.HostScreen> screen,
            ImageCopyTexture* texture,
            Slice<u8> data,
            Wgpu.ImageDataLayout* data_layout,
            Wgpu.Extent3d* size)
            => elffy_write_texture(screen, texture, data, data_layout, size).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static Box<Wgpu.BindGroupLayout> CreateBindGroupLayout(
            Ref<Elffycore.HostScreen> screen,
            BindGroupLayoutDescriptor* desc)
            => elffy_create_bind_group_layout(screen, desc).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DestroyBindGroupLayout(
            Box<Wgpu.BindGroupLayout> handle)
        {
            handle.ThrowIfInvalid();
            elffy_destroy_bind_group_layout(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static Box<Wgpu.BindGroup> CreateBindGroup(
            Ref<Elffycore.HostScreen> screen,
            BindGroupDescriptor* desc)
            => elffy_create_bind_group(screen, desc).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DestroyBindGroup(
            Box<Wgpu.BindGroup> handle)
        {
            handle.ThrowIfInvalid();
            elffy_destroy_bind_group(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static Box<Wgpu.PipelineLayout> CreatePipelineLayout(
            Ref<Elffycore.HostScreen> screen,
            PipelineLayoutDescriptor* desc)
            => elffy_create_pipeline_layout(screen, desc).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DestroyPipelineLayout(
            Box<Wgpu.PipelineLayout> handle)
        {
            handle.ThrowIfInvalid();
            elffy_destroy_pipeline_layout(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static Box<Wgpu.RenderPipeline> CreateRenderPipeline(
            Ref<Elffycore.HostScreen> screen,
            RenderPipelineDescriptor* desc)
            => elffy_create_render_pipeline(screen, desc).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DestroyRenderPipeline(
            Box<Wgpu.RenderPipeline> handle)
        {
            handle.ThrowIfInvalid();
            elffy_destroy_render_pipeline(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static Box<Wgpu.Buffer> CreateBufferInit(
            Ref<Elffycore.HostScreen> screen,
            Slice<u8> contents,
            Wgpu.BufferUsages usage)
            => elffy_create_buffer_init(screen, contents, usage).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DestroyBuffer(
            Box<Wgpu.Buffer> handle)
        {
            handle.ThrowIfInvalid();
            elffy_destroy_buffer(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static Box<Wgpu.Sampler> CreateSampler(
            Ref<Elffycore.HostScreen> screen,
            SamplerDescriptor* desc)
            => elffy_create_sampler(screen, desc).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DestroySampler(
            Box<Wgpu.Sampler> handle)
        {
            handle.ThrowIfInvalid();
            elffy_destroy_sampler(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static Box<Wgpu.ShaderModule> CreateShaderModule(
            Ref<Elffycore.HostScreen> screen,
            Slice<u8> shader_source)
            => elffy_create_shader_module(screen, shader_source).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DestroyShaderModule(
            Box<Wgpu.ShaderModule> handle)
        {
            handle.ThrowIfInvalid();
            elffy_destroy_shader_module(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static Box<Wgpu.Texture> CreateTexture(
            Ref<Elffycore.HostScreen> screen,
            TextureDescriptor* desc)
            => elffy_create_texture(screen, desc).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static Box<Wgpu.Texture> CreateTextureWithData(
            Ref<Elffycore.HostScreen> screen,
            TextureDescriptor* desc,
            Slice<u8> data)
            => elffy_create_texture_with_data(screen, desc, data).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DestroyTexture(
            Box<Wgpu.Texture> handle)
        {
            handle.ThrowIfInvalid();
            elffy_destroy_texture(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static Box<Wgpu.TextureView> CreateTextureView(
            Ref<Wgpu.Texture> texture,
            TextureViewDescriptor* desc)
            => elffy_create_texture_view(texture, desc).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DestroyTextureView(
            Box<Wgpu.TextureView> handle)
        {
            handle.ThrowIfInvalid();
            elffy_destroy_texture_view(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void WriteBuffer(
            Ref<Elffycore.HostScreen> screen,
            Ref<Wgpu.Buffer> buffer,
            u64 offset,
            Slice<u8> data)
        {
            screen.ThrowIfInvalid();
            buffer.ThrowIfInvalid();
            elffy_write_buffer(screen, buffer, offset, data).Validate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void SetPipeline(
            MutRef<Wgpu.RenderPass> render_pass,
            Ref<Wgpu.RenderPipeline> render_pipeline)
        {
            render_pipeline.ThrowIfInvalid();
            elffy_set_pipeline(render_pass, render_pipeline).Validate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void SetBindGroup(
            MutRef<Wgpu.RenderPass> render_pass,
            u32 index,
            Ref<Wgpu.BindGroup> bind_group)
        {
            bind_group.ThrowIfInvalid();
            elffy_set_bind_group(render_pass, index, bind_group).Validate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void SetVertexBuffer(
            MutRef<Wgpu.RenderPass> render_pass,
            u32 slot,
            BufferSlice buffer_slice)
        {
            buffer_slice.buffer.ThrowIfInvalid();
            elffy_set_vertex_buffer(render_pass, slot, buffer_slice).Validate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void SetIndexBuffer(
            MutRef<Wgpu.RenderPass> render_pass,
            BufferSlice buffer_slice,
            Wgpu.IndexFormat index_format)
        {
            buffer_slice.buffer.ThrowIfInvalid();
            elffy_set_index_buffer(render_pass, buffer_slice, index_format).Validate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void Draw(
            MutRef<Wgpu.RenderPass> render_pass,
            RangeU32 vertices,
            RangeU32 instances)
        {
            elffy_draw(render_pass, vertices, instances).Validate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DrawIndexed(
            MutRef<Wgpu.RenderPass> render_pass,
            RangeU32 indices,
            i32 base_vertex,
            RangeU32 instances)
        {
            elffy_draw_indexed(render_pass, indices, base_vertex, instances).Validate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerNonUserCode]
        private static void ThrowNativeErrorIfNotZero(nuint errorCount)
        {
            if(errorCount != 0) {
                ThrowNativeError(errorCount);

                [DoesNotReturn]
                [DebuggerNonUserCode]
                static void ThrowNativeError(nuint errorCount)
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
            private readonly nuint _errorCount;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [DebuggerHidden]
            public void Validate() => EngineCore.ThrowNativeErrorIfNotZero(_errorCount);
        }

        private readonly struct ApiBoxResult<T> where T : INativeTypeMarker
        {
            // (_errorCount, _nativePtr) is (0, not null) or (not 0, null)

            private readonly nuint _errorCount;
            private readonly void* _nativePtr;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            //[DebuggerHidden]
            public Box<T> Validate()
            {
                var nativePtr = _nativePtr;
                Debug.Assert((_errorCount == 0 && nativePtr != null) || (_errorCount > 0 && nativePtr == null));
                EngineCore.ThrowNativeErrorIfNotZero(_errorCount);
                Debug.Assert(_errorCount == 0);
                Debug.Assert(nativePtr != null);
                return *(Box<T>*)(&nativePtr);
            }
        }

        private readonly struct ApiValueResult<T> where T : unmanaged
        {
            private readonly nuint _errorCount;
            private readonly T _value;

            [UnscopedRef]
            public ref readonly T Validate()
            {
                EngineCore.ThrowNativeErrorIfNotZero(_errorCount);
                return ref _value;
            }
        }
    }

    internal record struct NativeError(Elffycore.ErrMessageId Id, string Message);

    internal sealed class EngineCoreException : Exception
    {
        //private readonly string[] _messages;
        private readonly NativeError[] _errors;

        public ReadOnlyMemory<NativeError> Errors => _errors;

        internal static EngineCoreException NewUnknownError() => new(ReadOnlySpan<NativeError>.Empty);
        internal EngineCoreException(ReadOnlySpan<NativeError> errors) : base(BuildExceptionMessage(errors))
        {
            _errors = errors.ToArray();
        }

        private static string BuildExceptionMessage(ReadOnlySpan<NativeError> errors)
        {
            if(errors.Length == 0) {
                return "Some error occurred in the native code, but the error message could not be retrieved.";
            }
            if(errors.Length == 1) {
                return errors[0].Message;
            }
            else {
                var sb = new StringBuilder($"Multiple errors occurred in the native code. (ErrorCount: {errors.Length}) \n");
                foreach(var err in errors) {
                    sb.AppendLine(err.Message);
                }
                return sb.ToString();
            }
        }
    }

    internal readonly struct EngineCoreConfig
    {
        public required Action<Box<Elffycore.HostScreen>, Elffycore.HostScreenInfo, Elffycore.HostScreenId> OnStart { get; init; }
        public required Action<Elffycore.HostScreenId> OnRedrawRequested { get; init; }
        public required Action<Elffycore.HostScreenId> OnCleared { get; init; }

        public required Action<Elffycore.HostScreenId, u32, u32> OnResized { get; init; }
    }

    internal readonly ref struct HostScreenConfig
    {
        public required WindowStyle Style { get; init; }
        public required u32 Width { get; init; }
        public required u32 Height { get; init; }
        public required Wgpu.Backends Backend { get; init; }
    }

    internal delegate void EngineCoreRenderAction(Ref<Elffycore.HostScreen> screen, MutRef<Wgpu.RenderPass> renderPass);
    internal delegate void EngineCoreResizedAction(Ref<Elffycore.HostScreen> screen, uint width, uint height);
    internal delegate Box<Wgpu.RenderPass> OnCommandBeginFunc(
        Ref<Elffycore.HostScreen> screen,
        Ref<Wgpu.TextureView> surfaceTextureView,
        MutRef<Wgpu.CommandEncoder> commandEncoder,
        CreateRenderPassFunc createRenderPass);

    internal delegate Box<Wgpu.RenderPass> CreateRenderPassFunc(MutRef<Wgpu.CommandEncoder> commandEncoder, in RenderPassDescriptor desc);


    public sealed class Never
    {
        private Never() => throw new UnreachableException();
    }
}
