using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using u8 = System.Byte;
using u32 = System.UInt32;
using u64 = System.UInt64;

namespace WgpuSample
{
    internal unsafe static partial class NativeApi
    {
        private const string Library = "wgpu_sample";

        private static Action<HostScreenHandle>? _init;
        private static Action<HostScreenHandle, RenderPassHandle>? _onRender;

        [DoesNotReturn]
        public static Never elffy_engine_start(Action<HostScreenHandle> init, Action<HostScreenHandle, RenderPassHandle> onRender)
        {
            _init = init;
            _onRender = onRender;
            elffy_engine_start(&OnInit);
            throw new UnreachableException();

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static HostScreenCallbacks OnInit(HostScreenHandle screen)
            {
                _init?.Invoke(screen);

                return new HostScreenCallbacks
                {
                    on_render = &OnRender,
                };
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static void OnRender(HostScreenHandle screen, RenderPassHandle render_pass)
            {
                _onRender?.Invoke(screen, render_pass);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        private struct HostScreenCallbacks
        {
            public static HostScreenCallbacks None => default;

            public delegate* unmanaged[Cdecl]<HostScreenHandle, RenderPassHandle, void> on_render;
        }

        [LibraryImport(Library), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static partial void elffy_engine_start(delegate* unmanaged[Cdecl]<HostScreenHandle, HostScreenCallbacks> init);

        [LibraryImport(Library), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial RenderPipelineHandle elffy_add_render_pipeline(HostScreenHandle screen, in RenderPipelineInfo render_pipeline);

        [LibraryImport(Library), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial BufferHandle elffy_create_buffer_init(HostScreenHandle screen, RustSlice<u8> contents, BufferUsages usage);

        [LibraryImport(Library), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void elffy_set_pipeline(RenderPassHandle render_pass, RenderPipelineHandle render_pipeline);

        [LibraryImport(Library), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void elffy_draw(
            RenderPassHandle render_pass,
            u32 slot,
            BufferHandle buffer,
            u64 buffer_slice_offset,
            u64 buffer_slice_size,
            u32 draw_offset,
            u32 draw_size,
            u32 instances_size
            );

        //[DebuggerHidden]
        //[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        //private static void OnError(byte* message, nuint len)
        //{
        //    var bytelen = (int)Math.Min(len, int.MaxValue);
        //    var messageStr = Encoding.UTF8.GetString(message, bytelen);
        //    throw new NativeApiException(messageStr);
        //}
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RenderPipelineInfo
    {
        public VertexLayoutInfo vertex;
        public RustSlice<u8> shader_source;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct RustSlice<T> where T : unmanaged
    {
        public T* ptr;
        public nuint len;

        public RustSlice(T* ptr, nuint len)
        {
            this.ptr = ptr;
            this.len = len;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct VertexLayoutInfo
    {
        public u64 vertex_size;
        public RustSlice<VertexAttribute> attributes;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct VertexAttribute
    {
        public VertexFormat format;
        public u64 offset;
        public u32 shader_location;
    }

    public enum BufferUsages : u32
    {
        MAP_READ = 1 << 0,
        MAP_WRITE = 1 << 1,
        COPY_SRC = 1 << 2,
        COPY_DST = 1 << 3,
        INDEX = 1 << 4,
        VERTEX = 1 << 5,
        UNIFORM = 1 << 6,
        STORAGE = 1 << 7,
        INDIRECT = 1 << 8,
    }

    public enum VertexFormat : u32
    {
        /// Two unsigned bytes (u8). `uvec2` in shaders.
        Uint8x2 = 0,
        /// Four unsigned bytes (u8). `uvec4` in shaders.
        Uint8x4 = 1,
        /// Two signed bytes (i8). `ivec2` in shaders.
        Sint8x2 = 2,
        /// Four signed bytes (i8). `ivec4` in shaders.
        Sint8x4 = 3,
        /// Two unsigned bytes (u8). [0, 255] converted to float [0, 1] `vec2` in shaders.
        Unorm8x2 = 4,
        /// Four unsigned bytes (u8). [0, 255] converted to float [0, 1] `vec4` in shaders.
        Unorm8x4 = 5,
        /// Two signed bytes (i8). [-127, 127] converted to float [-1, 1] `vec2` in shaders.
        Snorm8x2 = 6,
        /// Four signed bytes (i8). [-127, 127] converted to float [-1, 1] `vec4` in shaders.
        Snorm8x4 = 7,
        /// Two unsigned shorts (u16). `uvec2` in shaders.
        Uint16x2 = 8,
        /// Four unsigned shorts (u16). `uvec4` in shaders.
        Uint16x4 = 9,
        /// Two signed shorts (i16). `ivec2` in shaders.
        Sint16x2 = 10,
        /// Four signed shorts (i16). `ivec4` in shaders.
        Sint16x4 = 11,
        /// Two unsigned shorts (u16). [0, 65535] converted to float [0, 1] `vec2` in shaders.
        Unorm16x2 = 12,
        /// Four unsigned shorts (u16). [0, 65535] converted to float [0, 1] `vec4` in shaders.
        Unorm16x4 = 13,
        /// Two signed shorts (i16). [-32767, 32767] converted to float [-1, 1] `vec2` in shaders.
        Snorm16x2 = 14,
        /// Four signed shorts (i16). [-32767, 32767] converted to float [-1, 1] `vec4` in shaders.
        Snorm16x4 = 15,
        /// Two half-precision floats (no Rust equiv). `vec2` in shaders.
        Float16x2 = 16,
        /// Four half-precision floats (no Rust equiv). `vec4` in shaders.
        Float16x4 = 17,
        /// One single-precision float (f32). `float` in shaders.
        Float32 = 18,
        /// Two single-precision floats (f32). `vec2` in shaders.
        Float32x2 = 19,
        /// Three single-precision floats (f32). `vec3` in shaders.
        Float32x3 = 20,
        /// Four single-precision floats (f32). `vec4` in shaders.
        Float32x4 = 21,
        /// One unsigned int (u32). `uint` in shaders.
        Uint32 = 22,
        /// Two unsigned ints (u32). `uvec2` in shaders.
        Uint32x2 = 23,
        /// Three unsigned ints (u32). `uvec3` in shaders.
        Uint32x3 = 24,
        /// Four unsigned ints (u32). `uvec4` in shaders.
        Uint32x4 = 25,
        /// One signed int (i32). `int` in shaders.
        Sint32 = 26,
        /// Two signed ints (i32). `ivec2` in shaders.
        Sint32x2 = 27,
        /// Three signed ints (i32). `ivec3` in shaders.
        Sint32x3 = 28,
        /// Four signed ints (i32). `ivec4` in shaders.
        Sint32x4 = 29,
        /// One double-precision float (f64). `double` in shaders. Requires VERTEX_ATTRIBUTE_64BIT features.
        Float64 = 30,
        /// Two double-precision floats (f64). `dvec2` in shaders. Requires VERTEX_ATTRIBUTE_64BIT features.
        Float64x2 = 31,
        /// Three double-precision floats (f64). `dvec3` in shaders. Requires VERTEX_ATTRIBUTE_64BIT features.
        Float64x3 = 32,
        /// Four double-precision floats (f64). `dvec4` in shaders. Requires VERTEX_ATTRIBUTE_64BIT features.
        Float64x4 = 33,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe readonly struct HostScreenHandle : IEquatable<HostScreenHandle>
    {
        private readonly void* _handle;
        public static HostScreenHandle None => default;

        internal HostScreenHandle(void* handle) => _handle = handle;

        public override bool Equals(object? obj) => obj is HostScreenHandle handle && Equals(handle);

        public bool Equals(HostScreenHandle other) => _handle == other._handle;

        public override int GetHashCode() => ((IntPtr)_handle).GetHashCode();

        public static bool operator ==(HostScreenHandle left, HostScreenHandle right) => left.Equals(right);

        public static bool operator !=(HostScreenHandle left, HostScreenHandle right) => !(left == right);

        public static implicit operator HostScreenHandle(void* handle) => new(handle);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe readonly struct RenderPipelineHandle : IEquatable<RenderPipelineHandle>
    {
        private readonly void* _handle;
        public static RenderPipelineHandle None => default;

        internal RenderPipelineHandle(void* handle) => _handle = handle;

        public override bool Equals(object? obj) => obj is RenderPipelineHandle handle && Equals(handle);

        public bool Equals(RenderPipelineHandle other) => _handle == other._handle;

        public override int GetHashCode() => ((IntPtr)_handle).GetHashCode();

        public static bool operator ==(RenderPipelineHandle left, RenderPipelineHandle right) => left.Equals(right);

        public static bool operator !=(RenderPipelineHandle left, RenderPipelineHandle right) => !(left == right);

        public static implicit operator RenderPipelineHandle(void* handle) => new(handle);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe readonly struct RenderPassHandle : IEquatable<RenderPassHandle>
    {
        private readonly void* _handle;
        public static RenderPassHandle None => default;

        internal RenderPassHandle(void* handle) => _handle = handle;

        public override bool Equals(object? obj) => obj is RenderPassHandle handle && Equals(handle);

        public bool Equals(RenderPassHandle other) => _handle == other._handle;

        public override int GetHashCode() => ((IntPtr)_handle).GetHashCode();

        public static bool operator ==(RenderPassHandle left, RenderPassHandle right) => left.Equals(right);

        public static bool operator !=(RenderPassHandle left, RenderPassHandle right) => !(left == right);

        public static implicit operator RenderPassHandle(void* handle) => new(handle);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe readonly struct BufferHandle : IEquatable<BufferHandle>
    {
        private readonly void* _handle;
        public static BufferHandle None => default;

        internal BufferHandle(void* handle) => _handle = handle;

        public override bool Equals(object? obj) => obj is BufferHandle handle && Equals(handle);

        public bool Equals(BufferHandle other) => _handle == other._handle;

        public override int GetHashCode() => ((IntPtr)_handle).GetHashCode();

        public static bool operator ==(BufferHandle left, BufferHandle right) => left.Equals(right);

        public static bool operator !=(BufferHandle left, BufferHandle right) => !(left == right);

        public static implicit operator BufferHandle(void* handle) => new(handle);
    }

    public sealed class NativeApiException : Exception
    {
        public NativeApiException()
        {
        }

        public NativeApiException(string message) : base(message)
        {
        }
    }

    public sealed class Never
    {
        private Never() => throw new UnreachableException();
    }
}
