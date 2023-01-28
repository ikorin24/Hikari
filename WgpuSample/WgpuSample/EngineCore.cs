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
        private static EngineConfig _config;
        private static int _isStarted = 0;

        [DoesNotReturn]
        public static Never EngineStart(in EngineConfig config)
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
            var engineConfig = new EngineCoreConfig
            {
                on_screen_init = new(&OnScreenInit),
                err_dispatcher = new(&DispatchError),
                on_command_begin = new(&OnCommandBegin),
                on_resized = new(&OnResized),
            };
            var screenConfig = new HostScreenConfig
            {
                backend = wgpu_Backends.ALL,
                width = 1280,
                height = 720,
                style = WindowStyle.Default,
                title = Slice.FromFixedSpanUnsafe("Elffy"u8),
            };

            Debug.Assert(engineConfig.on_screen_init.IsNull == false);
            Debug.Assert(engineConfig.err_dispatcher.IsNull == false);
            elffy_engine_start(&engineConfig, &screenConfig);
            throw new UnreachableException();

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static void OnScreenInit(Ref<Elffycore.HostScreen> screen, HostScreenInfo* info)
            {
                _config.OnStart(screen, in *info);
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static void DispatchError(ErrMessageId id, byte* messagePtr, nuint messageByteLen)
            {
                var len = (int)nuint.Min(messageByteLen, (nuint)int.MaxValue);
                var message = Encoding.UTF8.GetString(messagePtr, len);
                _nativeErrorStore ??= new();
                _nativeErrorStore.Add(new(id, message));
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static void OnCommandBegin(Ref<Elffycore.HostScreen> screen, Ref<Wgpu.TextureView> surface_texture_view, MutRef<Wgpu.CommandEncoder> command_encoder)
            {
                const int ColorAttachmentCount = 1;
                //var colorAttachments = stackalloc Opt<RenderPassColorAttachment>[ColorAttachmentCount]
                //{
                //    Opt.Some(new RenderPassColorAttachment
                //    {
                //        view = surface_texture_view,
                //        clear = new wgpu_Color(0, 0, 0, 0),
                //    }),
                //};
                //var desc = new RenderPassDescriptor
                //{
                //    color_attachments_clear = new(colorAttachments, (nuint)ColorAttachmentCount),
                //    depth_stencil_attachment_clear = Opt.None<RenderPassDepthStencilAttachment>(),
                //};

                var colorAttachments = stackalloc Opt_RenderPassColorAttachment[ColorAttachmentCount]
                {
                    new()
                    {
                        exists = true,
                        value = new RenderPassColorAttachment
                        {
                            view = surface_texture_view,
                            clear = new wgpu_Color(0, 0, 0, 0),
                        },
                    },
                };
                var desc = new RenderPassDescriptor
                {
                    color_attachments_clear = new()
                    {
                        data = colorAttachments,
                        len = ColorAttachmentCount,
                    },
                    depth_stencil_attachment_clear = Opt_RenderPassDepthStencilAttachment.None,
                };

                // Use renderPass here.
                // ...
                var renderPass = _config.OnCommandBegin(
                    screen,
                    surface_texture_view,
                    command_encoder,
                    _createRenderPassFunc);
                _config.OnRender(screen, renderPass);
                elffy_destroy_render_pass(renderPass);
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static void OnResized(Ref<Elffycore.HostScreen> screen, uint width, uint height) => _config.OnResized(screen, width, height);
        }

        private static readonly CreateRenderPassFunc _createRenderPassFunc =
            (MutRef<Wgpu.CommandEncoder> command_encoder, in RenderPassDescriptor desc) =>
            {
                fixed(RenderPassDescriptor* descPtr = &desc) {
                    return elffy_create_render_pass(command_encoder, descPtr).Validate();
                }
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static (u32 Width, u32 Height) ScreenGetInnerSize(
            Ref<Elffycore.HostScreen> screen)
        {
            u32 width;
            u32 height;
            elffy_screen_get_inner_size(screen, &width, &height).Validate();
            return (width, height);
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
            wgpu_ImageDataLayout* data_layout,
            wgpu_Extent3d* size)
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
            wgpu_BufferUsages usage)
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
            wgpu_IndexFormat index_format)
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

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct ApiResult
        {
            private readonly nuint _errorCount;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [DebuggerHidden]
            public void Validate() => EngineCore.ThrowNativeErrorIfNotZero(_errorCount);
        }

        //[StructLayout(LayoutKind.Sequential)]
        //private readonly struct ApiBoxResult<THandle> where THandle : unmanaged, IHandle<THandle>
        //{
        //    // (_errorCount, _nativePtr) is (0, not null) or (not 0, null)

        //    private readonly uint _errorCount;
        //    private readonly void* _nativePtr;

        //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //    //[DebuggerHidden]
        //    public THandle Validate()
        //    {
        //        Debug.Assert((_errorCount == 0 && _nativePtr != null) || (_errorCount > 0 && _nativePtr == null));
        //        EngineCore.ThrowNativeErrorIfNotZero(_errorCount);
        //        Debug.Assert(_errorCount == 0);
        //        Debug.Assert(_nativePtr != null);
        //        return (THandle)_nativePtr;
        //    }
        //}

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct ApiBoxResult<T> where T : INativeTypeMarker
        {
            // (_errorCount, _nativePtr) is (0, not null) or (not 0, null)

            private readonly uint _errorCount;
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
    }

    internal record struct NativeError(ErrMessageId Id, string Message);

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

    internal readonly struct EngineConfig
    {
        public required EngineCoreStartAction OnStart { get; init; }
        public required EngineCoreRenderAction OnRender { get; init; }
        public required EngineCoreResizedAction OnResized { get; init; }
        public required OnCommandBeginFunc OnCommandBegin { get; init; }
    }

    internal delegate void EngineCoreStartAction(Ref<Elffycore.HostScreen> screen, in HostScreenInfo info);
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
