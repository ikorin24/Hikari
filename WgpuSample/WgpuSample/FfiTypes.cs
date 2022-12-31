#nullable enable
using System;
using System.Runtime.InteropServices;
using u8 = System.Byte;
using u16 = System.UInt16;
using u32 = System.UInt32;
using u64 = System.UInt64;

namespace WgpuSample;

[StructLayout(LayoutKind.Sequential)]
internal record struct HostScreenHandle(Handle Handle);

[StructLayout(LayoutKind.Sequential)]
internal record struct RenderPipelineHandle(Handle Handle);

[StructLayout(LayoutKind.Sequential)]
internal record struct RenderPassHandle(Handle Handle);

[StructLayout(LayoutKind.Sequential)]
internal record struct BufferHandle(Handle Handle);

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct HostScreenCallbacks
{
    public static HostScreenCallbacks None => default;

    public delegate* unmanaged[Cdecl]<HostScreenHandle, RenderPassHandle, void> on_render;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RenderPipelineInfo
{
    public VertexLayoutInfo vertex;
    public Sliceffi<u8> shader_source;
}

[StructLayout(LayoutKind.Sequential)]
internal struct BufferSliceffi
{
    public BufferHandle buffer;
    public RangeBoundsU64ffi range;
    public BufferSliceffi(BufferHandle buffer, RangeBoundsU64ffi range) => (this.buffer, this.range) = (buffer, range);
}

[StructLayout(LayoutKind.Sequential)]
internal struct SlotBufferSliceffi
{
    public BufferSliceffi buffer_slice;
    public u32 slot;
}

[StructLayout(LayoutKind.Sequential)]
internal struct IndexBufferSliceffi
{
    public BufferSliceffi buffer_slice;
    public IndexFormat format;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct Sliceffi<T> where T : unmanaged
{
    public T* ptr;
    public nuint len;

    public Sliceffi(T* ptr, nuint len)
    {
        this.ptr = ptr;
        this.len = len;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct RangeU64ffi
{
    public u64 start;
    public u64 end_excluded;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RangeU32ffi
{
    public u32 start;
    public u32 end_excluded;
    public RangeU32ffi(u32 start, u32 end_excluded) => (this.start, this.end_excluded) = (start, end_excluded);
}

[StructLayout(LayoutKind.Sequential)]
internal struct RangeBoundsU64ffi
{
    u64 start;
    u64 end_excluded;
    bool has_start;
    bool has_end_excluded;
    public static RangeBoundsU64ffi All => default;
    public RangeBoundsU64ffi(u64? start, u64? end_excluded)
    {
        (this.start, this.has_start) = start.HasValue switch
        {
            true => (start.Value, true),
            false => (default, false),
        };
        (this.end_excluded, this.has_end_excluded) = end_excluded.HasValue switch
        {
            true => (end_excluded.Value, true),
            false => (default, false),
        };
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct VertexLayoutInfo
{
    public u64 vertex_size;
    public Sliceffi<VertexAttribute> attributes;
}

[StructLayout(LayoutKind.Sequential)]
internal struct VertexAttribute
{
    public VertexFormat format;
    public u64 offset;
    public u32 shader_location;
}

internal enum BufferUsages : u32
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

internal enum VertexFormat : u32
{
    /// <summary>/// Two unsigned bytes (u8). `uvec2` in shaders.</summary>
    Uint8x2 = 0,
    /// <summary>/// Four unsigned bytes (u8). `uvec4` in shaders.</summary>
    Uint8x4 = 1,
    /// <summary>/// Two signed bytes (i8). `ivec2` in shaders.</summary>
    Sint8x2 = 2,
    /// <summary>/// Four signed bytes (i8). `ivec4` in shaders.</summary>
    Sint8x4 = 3,
    /// <summary>/// Two unsigned bytes (u8). [0, 255] converted to float [0, 1] `vec2` in shaders.</summary>
    Unorm8x2 = 4,
    /// <summary>/// Four unsigned bytes (u8). [0, 255] converted to float [0, 1] `vec4` in shaders.</summary>
    Unorm8x4 = 5,
    /// <summary>/// Two signed bytes (i8). [-127, 127] converted to float [-1, 1] `vec2` in shaders.</summary>
    Snorm8x2 = 6,
    /// <summary>/// Four signed bytes (i8). [-127, 127] converted to float [-1, 1] `vec4` in shaders.</summary>
    Snorm8x4 = 7,
    /// <summary>/// Two unsigned shorts (u16). `uvec2` in shaders.</summary>
    Uint16x2 = 8,
    /// <summary>/// Four unsigned shorts (u16). `uvec4` in shaders.</summary>
    Uint16x4 = 9,
    /// <summary>/// Two signed shorts (i16). `ivec2` in shaders.</summary>
    Sint16x2 = 10,
    /// <summary>/// Four signed shorts (i16). `ivec4` in shaders.</summary>
    Sint16x4 = 11,
    /// <summary>/// Two unsigned shorts (u16). [0, 65535] converted to float [0, 1] `vec2` in shaders.</summary>
    Unorm16x2 = 12,
    /// <summary>/// Four unsigned shorts (u16). [0, 65535] converted to float [0, 1] `vec4` in shaders.</summary>
    Unorm16x4 = 13,
    /// <summary>/// Two signed shorts (i16). [-32767, 32767] converted to float [-1, 1] `vec2` in shaders.</summary>
    Snorm16x2 = 14,
    /// <summary>/// Four signed shorts (i16). [-32767, 32767] converted to float [-1, 1] `vec4` in shaders.</summary>
    Snorm16x4 = 15,
    /// <summary>/// Two half-precision floats (no Rust equiv). `vec2` in shaders.</summary>
    Float16x2 = 16,
    /// <summary>/// Four half-precision floats (no Rust equiv). `vec4` in shaders.</summary>
    Float16x4 = 17,
    /// <summary>/// One single-precision float (f32). `float` in shaders.</summary>
    Float32 = 18,
    /// <summary>/// Two single-precision floats (f32). `vec2` in shaders.</summary>
    Float32x2 = 19,
    /// <summary>/// Three single-precision floats (f32). `vec3` in shaders.</summary>
    Float32x3 = 20,
    /// <summary>/// Four single-precision floats (f32). `vec4` in shaders.</summary>
    Float32x4 = 21,
    /// <summary>/// One unsigned int (u32). `uint` in shaders.</summary>
    Uint32 = 22,
    /// <summary>/// Two unsigned ints (u32). `uvec2` in shaders.</summary>
    Uint32x2 = 23,
    /// <summary>/// Three unsigned ints (u32). `uvec3` in shaders.</summary>
    Uint32x3 = 24,
    /// <summary>/// Four unsigned ints (u32). `uvec4` in shaders.</summary>
    Uint32x4 = 25,
    /// <summary>/// One signed int (i32). `int` in shaders.</summary>
    Sint32 = 26,
    /// <summary>/// Two signed ints (i32). `ivec2` in shaders.</summary>
    Sint32x2 = 27,
    /// <summary>/// Three signed ints (i32). `ivec3` in shaders.</summary>
    Sint32x3 = 28,
    /// <summary>/// Four signed ints (i32). `ivec4` in shaders.</summary>
    Sint32x4 = 29,
    /// <summary>/// One double-precision float (f64). `double` in shaders. Requires VERTEX_ATTRIBUTE_64BIT features.</summary>
    Float64 = 30,
    /// <summary>/// Two double-precision floats (f64). `dvec2` in shaders. Requires VERTEX_ATTRIBUTE_64BIT features.</summary>
    Float64x2 = 31,
    /// <summary>/// Three double-precision floats (f64). `dvec3` in shaders. Requires VERTEX_ATTRIBUTE_64BIT features.</summary>
    Float64x3 = 32,
    /// <summary>/// Four double-precision floats (f64). `dvec4` in shaders. Requires VERTEX_ATTRIBUTE_64BIT features.</summary>
    Float64x4 = 33,
}

internal enum IndexFormat : u32
{
    /// <summary>Indices are 16 bit unsigned integers.</summary>
    Uint16 = 0,
    /// <summary>Indices are 32 bit unsigned integers.</summary>
    Uint32 = 1,
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe readonly struct Handle : IEquatable<Handle>
{
    private readonly void* _handle;

    public override bool Equals(object? obj) => obj is Handle handle && Equals(handle);

    public bool Equals(Handle other) => _handle == other._handle;

    public override int GetHashCode() => ((IntPtr)_handle).GetHashCode();

    public static bool operator ==(Handle left, Handle right) => left.Equals(right);

    public static bool operator !=(Handle left, Handle right) => !(left == right);

    public override string ToString() => ((IntPtr)_handle).ToString();
}
