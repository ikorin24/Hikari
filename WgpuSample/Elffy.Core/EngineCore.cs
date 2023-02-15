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

namespace Elffy
{
    internal unsafe static partial class EngineCore
    {
        [ThreadStatic]
        private static List<NativeError>? _nativeErrorStore;
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
            var engineConfigNative = new CE.EngineCoreConfig
            {
                err_dispatcher = new(&DispatchError),
                on_screen_init = new(&OnScreenInit),
                event_cleared = new(&EventCleared),
                event_redraw_requested = new(&EventRedrawRequested),
                event_resized = new(&EventResized),
            };

            var screenConfigNative = screenConfig.ToCoreType();

            var errorCount = elffy_engine_start(&engineConfigNative, &screenConfigNative);
            Debug.Assert(errorCount > 0);
            ThrowNativeErrorIfNotZero(errorCount);

            throw new UnreachableException();


            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static void OnScreenInit(
                void* screen_,  // Rust.Box<CE.HostScreen> screen
                CE.HostScreenInfo* info,
                CE.HostScreenId id
                )
            {
                // UnmanagedCallersOnly methods cannot have generic type args.
                Rust.Box<CE.HostScreen> screen = *(Rust.Box<CE.HostScreen>*)(&screen_);

                _config.OnStart(screen, *info, id);
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static void EventCleared(CE.HostScreenId id)
            {
                _config.OnCleared(id);
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static void EventRedrawRequested(CE.HostScreenId id)
            {
                _config.OnRedrawRequested(id);
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static void EventResized(CE.HostScreenId id, u32 width, u32 height)
            {
                _config.OnResized(id, width, height);
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

        private static readonly CreateRenderPassFunc _createRenderPassFunc =
            (Rust.MutRef<Wgpu.CommandEncoder> command_encoder, in CE.RenderPassDescriptor desc) =>
            {
                fixed(CE.RenderPassDescriptor* descPtr = &desc) {
                    return elffy_create_render_pass(command_encoder, descPtr).Validate();
                }
            };

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
            this Rust.Ref<CE.HostScreen> screen,
            out Rust.Box<Wgpu.CommandEncoder> commandEncoder,
            out Rust.Box<Wgpu.SurfaceTexture> surfaceTexture,
            out Rust.Box<Wgpu.TextureView> surfaceTextureView)
        {
            ref readonly var data = ref elffy_screen_begin_command(screen).Validate();
            commandEncoder = data.command_encoder.UnwrapUnchecked();
            surfaceTexture = data.surface_texture.UnwrapUnchecked();
            surfaceTextureView = data.surface_texture_view.UnwrapUnchecked();
            return data.success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ScreenFinishCommand(
            this Rust.Ref<CE.HostScreen> screen,
            Rust.Box<Wgpu.CommandEncoder> commandEncoder,
            Rust.Box<Wgpu.SurfaceTexture> surfaceTexture,
            Rust.Box<Wgpu.TextureView> surfaceTextureView)
        {
            elffy_screen_finish_command(screen, commandEncoder, surfaceTexture, surfaceTextureView).Validate();
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
        //[DebuggerHidden]
        public static (u32 Width, u32 Height) ScreenGetInnerSize(
            this Rust.Ref<CE.HostScreen> screen)
        {
            var size = elffy_screen_get_inner_size(screen).Validate();
            return (size.width, size.height);
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
            Rust.Ref<CE.HostScreen> screen,
            CE.ImageCopyTexture* texture,
            CE.Slice<u8> data,
            Wgpu.ImageDataLayout* data_layout,
            Wgpu.Extent3d* size)
            => elffy_write_texture(screen, texture, data, data_layout, size).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static Rust.Box<Wgpu.BindGroupLayout> CreateBindGroupLayout(
            Rust.Ref<CE.HostScreen> screen,
            CE.BindGroupLayoutDescriptor* desc)
            => elffy_create_bind_group_layout(screen, desc).Validate();

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
            CE.BindGroupDescriptor* desc)
            => elffy_create_bind_group(screen, desc).Validate();

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
            Rust.Ref<CE.HostScreen> screen,
            CE.PipelineLayoutDescriptor* desc)
            => elffy_create_pipeline_layout(screen, desc).Validate();

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
            Rust.Ref<CE.HostScreen> screen,
            CE.RenderPipelineDescriptor* desc)
            => elffy_create_render_pipeline(screen, desc).Validate();

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
            Rust.Ref<CE.HostScreen> screen,
            CE.SamplerDescriptor* desc)
            => elffy_create_sampler(screen, desc).Validate();

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
            CE.Slice<u8> shader_source)
            => elffy_create_shader_module(screen, shader_source).Validate();

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
            Rust.Ref<CE.HostScreen> screen,
            CE.TextureDescriptor* desc)
            => elffy_create_texture(screen, desc).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static Rust.Box<Wgpu.Texture> CreateTextureWithData(
            Rust.Ref<CE.HostScreen> screen,
            CE.TextureDescriptor* desc,
            CE.Slice<u8> data)
            => elffy_create_texture_with_data(screen, desc, data).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DestroyTexture(
            this Rust.Box<Wgpu.Texture> handle)
        {
            handle.ThrowIfInvalid();
            elffy_destroy_texture(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static Rust.Box<Wgpu.TextureView> CreateTextureView(
            Rust.Ref<Wgpu.Texture> texture,
            CE.TextureViewDescriptor* desc)
            => elffy_create_texture_view(texture, desc).Validate();

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
            Rust.Ref<CE.HostScreen> screen,
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

    internal record struct NativeError(CE.ErrMessageId Id, string Message);

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

    internal delegate void EngineCoreRenderAction(Rust.Ref<CE.HostScreen> screen, Rust.MutRef<Wgpu.RenderPass> renderPass);
    internal delegate void EngineCoreResizedAction(Rust.Ref<CE.HostScreen> screen, uint width, uint height);
    internal delegate Rust.Box<Wgpu.RenderPass> OnCommandBeginFunc(
        Rust.Ref<CE.HostScreen> screen,
        Rust.Ref<Wgpu.TextureView> surfaceTextureView,
        Rust.MutRef<Wgpu.CommandEncoder> commandEncoder,
        CreateRenderPassFunc createRenderPass);

    internal delegate Rust.Box<Wgpu.RenderPass> CreateRenderPassFunc(Rust.MutRef<Wgpu.CommandEncoder> commandEncoder, in CE.RenderPassDescriptor desc);


    public sealed class Never
    {
        private Never() => throw new UnreachableException();
    }
}
