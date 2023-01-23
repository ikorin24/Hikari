#nullable enable
using u8 = System.Byte;
using u32 = System.UInt32;
using i32 = System.Int32;
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
using System.Runtime.ExceptionServices;
using System.Collections.Concurrent;

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
                on_screen_init = new(&OnInit),
                err_dispatcher = new(&DispatchError),
            };
            var screenConfig = new HostScreenConfig
            {
                backend = wgpu_Backends.ALL,
                width = 1280,
                height = 720,
                style = WindowStyle.Default,
                title = Slice.FromFixedSpanUnsafe("Elffy"u8),
            };
            elffy_engine_start(&engineConfig, &screenConfig);
            throw new UnreachableException();

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static HostScreenCallbacks OnInit(HostScreenHandle screen, HostScreenInfo* info)
            {
                _config.OnStart(screen, in *info);

                return new HostScreenCallbacks
                {
                    on_render = &OnRender,
                };
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
            static void OnRender(HostScreenHandle screen, RenderPassRef render_pass) => _config.OnRender(screen, render_pass);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void WriteTexture(
            HostScreenHandle screen,
            ImageCopyTexture* texture,
            Slice<u8> data,
            wgpu_ImageDataLayout* data_layout,
            wgpu_Extent3d* size)
            => elffy_write_texture(screen, texture, data, data_layout, size).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static BindGroupLayoutHandle CreateBindGroupLayout(
            HostScreenHandle screen,
            BindGroupLayoutDescriptor* desc)
            => elffy_create_bind_group_layout(screen, desc).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DestroyBindGroupLayout(
            BindGroupLayoutHandle handle)
        {
            handle.ThrowIfDestroyed();
            elffy_destroy_bind_group_layout(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static BindGroupHandle CreateBindGroup(
            HostScreenHandle screen,
            BindGroupDescriptor* desc)
            => elffy_create_bind_group(screen, desc).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DestroyBindGroup(
            BindGroupHandle handle)
        {
            handle.ThrowIfDestroyed();
            elffy_destroy_bind_group(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static PipelineLayoutHandle CreatePipelineLayout(
            HostScreenHandle screen,
            PipelineLayoutDescriptor* desc)
            => elffy_create_pipeline_layout(screen, desc).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DestroyPipelineLayout(
            PipelineLayoutHandle handle)
        {
            handle.ThrowIfDestroyed();
            elffy_destroy_pipeline_layout(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static RenderPipelineHandle CreateRenderPipeline(
            HostScreenHandle screen,
            RenderPipelineDescriptor* desc)
            => elffy_create_render_pipeline(screen, desc).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DestroyRenderPipeline(
            RenderPipelineHandle handle)
        {
            handle.ThrowIfDestroyed();
            elffy_destroy_render_pipeline(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static BufferHandle CreateBufferInit(
            HostScreenHandle screen,
            Slice<u8> contents,
            wgpu_BufferUsages usage)
            => elffy_create_buffer_init(screen, contents, usage).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DestroyBuffer(
            BufferHandle handle)
        {
            handle.ThrowIfDestroyed();
            elffy_destroy_buffer(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static SamplerHandle CreateSampler(
            HostScreenHandle screen,
            SamplerDescriptor* desc)
            => elffy_create_sampler(screen, desc).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DestroySampler(
            SamplerHandle handle)
        {
            handle.ThrowIfDestroyed();
            elffy_destroy_sampler(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static ShaderModuleHandle CreateShaderModule(
            HostScreenHandle screen,
            Slice<u8> shader_source)
            => elffy_create_shader_module(screen, shader_source).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DestroyShaderModule(
            ShaderModuleHandle handle)
        {
            handle.ThrowIfDestroyed();
            elffy_destroy_shader_module(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static TextureHandle CreateTexture(
            HostScreenHandle screen,
            TextureDescriptor* desc)
            => elffy_create_texture(screen, desc).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static TextureHandle CreateTextureWithData(
            HostScreenHandle screen,
            TextureDescriptor* desc,
            Slice<u8> data)
            => elffy_create_texture_with_data(screen, desc, data).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DestroyTexture(
            TextureHandle handle)
        {
            handle.ThrowIfDestroyed();
            elffy_destroy_texture(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static TextureViewHandle CreateTextureView(
            HostScreenHandle screen,
            TextureHandle texture,
            TextureViewDescriptor* desc)
            => elffy_create_texture_view(screen, texture, desc).Validate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DestroyTextureView(
            TextureViewHandle handle)
        {
            handle.ThrowIfDestroyed();
            elffy_destroy_texture_view(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void WriteBuffer(
            HostScreenHandle screen,
            BufferHandle buffer,
            u64 offset,
            Slice<u8> data)
        {
            screen.ThrowIfDestroyed();
            buffer.ThrowIfDestroyed();
            elffy_write_buffer(screen, buffer, offset, data).Validate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void SetPipeline(
            RenderPassRef render_pass,
            RenderPipelineHandle render_pipeline)
        {
            render_pipeline.ThrowIfDestroyed();
            elffy_set_pipeline(render_pass, render_pipeline).Validate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void SetBindGroup(
            RenderPassRef render_pass,
            u32 index,
            BindGroupHandle bind_group)
        {
            bind_group.ThrowIfDestroyed();
            elffy_set_bind_group(render_pass, index, bind_group).Validate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void SetVertexBuffer(
            RenderPassRef render_pass,
            u32 slot,
            BufSlice buffer_slice)
        {
            buffer_slice.buffer.ThrowIfDestroyed();
            elffy_set_vertex_buffer(render_pass, slot, buffer_slice).Validate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void SetIndexBuffer(
            RenderPassRef render_pass,
            BufSlice buffer_slice,
            wgpu_IndexFormat index_format)
        {
            buffer_slice.buffer.ThrowIfDestroyed();
            elffy_set_index_buffer(render_pass, buffer_slice, index_format).Validate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void Draw(
            RenderPassRef render_pass,
            RangeU32 vertices,
            RangeU32 instances)
        {
            elffy_draw(render_pass, vertices, instances).Validate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void DrawIndexed(
            RenderPassRef render_pass,
            RangeU32 indices,
            i32 base_vertex,
            RangeU32 instances)
        {
            elffy_draw_indexed(render_pass, indices, base_vertex, instances).Validate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        private static void ThrowNativeErrorIfNotZero(nuint errorCount)
        {
            if(errorCount != 0) {
                ThrowNativeError(errorCount);

                [DoesNotReturn]
                [DebuggerHidden]
                static void ThrowNativeError(nuint errorCount)
                {
                    Debug.Assert(errorCount != 0);
                    var store = _nativeErrorStore;
                    string[] messages;
                    if(store == null) {
                        messages = new string[1] { "Some error occurred in the native code, but the error message could not be retrieved." };
                    }
                    else {
                        Debug.Assert((nuint)store.Count == errorCount);
                        messages = new string[store.Count];
                        for(int i = 0; i < messages.Length; i++) {
                            messages[i] = store[i].Message;
                        }
                        store.Clear();
                    }
                    throw new EngineCoreException(messages);
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

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct ApiRefResult<THandle> where THandle : unmanaged, IHandle<THandle>
        {
            // (_errorCount, _nativePtr) is (0, not null) or (not 0, null)

            private readonly uint _errorCount;
            private readonly void* _nativePtr;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [DebuggerHidden]
            public THandle Validate()
            {
                Debug.Assert((_errorCount == 0 && _nativePtr != null) || (_errorCount > 0 && _nativePtr == null));
                EngineCore.ThrowNativeErrorIfNotZero(_errorCount);
                Debug.Assert(_errorCount == 0);
                Debug.Assert(_nativePtr != null);
                return (THandle)_nativePtr;
            }
        }

        private record struct NativeError(ErrMessageId Id, string Message);
    }

    public sealed class EngineCoreException : Exception
    {
        private readonly string[] _messages;

        public ReadOnlyMemory<string> Messages => _messages;

        internal EngineCoreException(string[] messages) : base(BuildExceptionMessage(messages))
        {
            _messages = messages;
        }

        private static string BuildExceptionMessage(string[] messages)
        {
            if(messages.Length == 1) {
                return messages[0];
            }
            else {
                var sb = new StringBuilder($"Multiple errors occurred in the native code. (ErrorCount: {messages.Length}) \n");
                foreach(var m in messages) {
                    sb.AppendLine(m);
                }
                return sb.ToString();
            }
        }
    }

    internal readonly struct EngineConfig
    {
        public required EngineCoreStartAction OnStart { get; init; }
        public required EngineCoreRenderAction OnRender { get; init; }
    }

    internal delegate void EngineCoreStartAction(HostScreenHandle screen, in HostScreenInfo info);
    internal delegate void EngineCoreRenderAction(HostScreenHandle screen, RenderPassRef renderPass);


    public sealed class Never
    {
        private Never() => throw new UnreachableException();
    }
}
