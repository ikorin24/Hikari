﻿#nullable enable
using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Hikari.Gltf.Internal;

namespace Hikari.Gltf.Parsing;

internal unsafe static class GltfParser
{
    private const uint ChunkType_Json = 0x4E4F534A;
    private const uint ChunkType_Bin = 0x004E4942;

    public static GltfObject ParseGltfFile(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        using var handle = File.OpenHandle(filePath);
        var fileSize = (nuint)RandomAccess.GetLength(handle);
        using var buf = new NativeBuffer(fileSize);
        if(buf.TryGetSpan(out var span)) {
            RandomAccess.Read(handle, span, 0);
            var gltf = JsonSerializer.Deserialize(span, GltfObjectContext.Default.GltfObject);
            ThrowIfGltfIsNull(gltf);
            return gltf;
        }
        else {
            var memories = buf.GetMemories();
            RandomAccess.Read(handle, memories, 0);
            using var stream = buf.GetStream();
            var gltf = JsonSerializer.Deserialize(stream, GltfObjectContext.Default.GltfObject);
            ThrowIfGltfIsNull(gltf);
            return gltf;
        }
    }

    public static GltfObject ParseGltf(ReadOnlySpan<byte> data)
    {
        var gltf = JsonSerializer.Deserialize(data, GltfObjectContext.Default.GltfObject);
        ThrowIfGltfIsNull(gltf);
        return gltf;
    }

    public static GltfObject ParseGltf(void* data, nuint length)
    {
        if(length > int.MaxValue) {
            using var stream = new PointerMemoryStream((byte*)data, length);
            var gltf = JsonSerializer.Deserialize(stream, GltfObjectContext.Default.GltfObject);
            ThrowIfGltfIsNull(gltf);
            return gltf;
        }
        else {
            var span = MemoryMarshal.CreateReadOnlySpan(ref *(byte*)data, (int)length);
            var gltf = JsonSerializer.Deserialize(span, GltfObjectContext.Default.GltfObject);
            ThrowIfGltfIsNull(gltf);
            return gltf;
        }
    }

    private static GltfObject ParseGltfPrivate(in byte data, nuint length)
    {
        if(length > int.MaxValue) {
            fixed(byte* ptr = &data) {
                using var stream = new PointerMemoryStream(ptr, length);
                var gltf = JsonSerializer.Deserialize(stream, GltfObjectContext.Default.GltfObject);
                ThrowIfGltfIsNull(gltf);
                return gltf;
            }
        }
        else {
            var span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in data), (int)length);
            var gltf = JsonSerializer.Deserialize(span, GltfObjectContext.Default.GltfObject);
            ThrowIfGltfIsNull(gltf);
            return gltf;
        }
    }

    public static GlbObject ParseGlbFile(string filePath, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        using var handle = File.OpenHandle(filePath);
        var fileSize = (nuint)RandomAccess.GetLength(handle);
        using var buf = new NativeBuffer(fileSize);
        if(buf.TryGetSpan(out var span)) {
            RandomAccess.Read(handle, span, 0);
            return ParseGlb(span, ct);
        }
        else {
            var memories = buf.GetMemories();
            RandomAccess.Read(handle, memories, 0);
            return ParseGlb(buf.Ptr, buf.ByteLength, ct);
        }
    }

    public static GlbObject ParseGlb(ResourceFile file, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if(file.TryGetHandle(out var handle)) {
            var len = (nuint)file.FileSize;
            void* ptr = NativeMemory.Alloc(len);
            try {
                handle.Read(ptr, len, 0);
                return ParseGlb(ptr, len, ct);
            }
            finally {
                NativeMemory.Free(ptr);
            }
        }
        else {
            nuint len = (nuint)file.FileSize;
            byte* ptr = (byte*)NativeMemory.Alloc(len);
            try {
                using var stream = file.GetStream();
                ulong pos = 0;
                while(true) {
                    ct.ThrowIfCancellationRequested();
                    int spanLen = (int)Math.Min(int.MaxValue, len - pos);
                    var span = new Span<byte>(ptr + pos, spanLen);
                    var readlen = stream.Read(span);
                    pos += (ulong)readlen;
                    if(readlen == 0) { break; }
                }
                return ParseGlb(ptr, len, ct);
            }
            finally {
                NativeMemory.Free(ptr);
            }
        }
    }

    public static GlbObject ParseGlb(ReadOnlySpan<byte> data, CancellationToken ct)
    {
        return ParseGlbCore(in MemoryMarshal.GetReference(data), (nuint)data.Length, ct);
    }

    public static GlbObject ParseGlb(void* data, nuint length, CancellationToken ct)
    {
        return ParseGlbCore(in *(byte*)data, length, ct);
    }

    private static GlbObject ParseGlbCore(in byte data, nuint length, CancellationToken ct)
    {
        // https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#glb-stored-buffer

        ct.ThrowIfCancellationRequested();
        GlbObject? glb = null;
        try {
            // --- Header [12 bytes] ---
            var header = new GlbHeader
            {
                Magic = ReadUInt32(in data, 0),
                Version = ReadUInt32(in data, 4),
                Length = ReadUInt32(in data, 8),
            };
            nuint pos;

            // --- Chunk 0 (JSON Chunk) ---
            {
                uint chunkLen = ReadUInt32(in data, 12);
                uint chunkType = ReadUInt32(in data, 16);
                if(chunkType != ChunkType_Json) {
                    throw new FormatException("Chunk type must be 'JSON' (chunk 0)");
                }
                var gltf = ParseGltfPrivate(in UnsafeEx.AddByteOffset(data, 20), chunkLen);
                pos = 20 + chunkLen;
                glb = new GlbObject(gltf);
            }

            ct.ThrowIfCancellationRequested();
            // --- Chunk 1 (Binary Buffer) ---
            {
                pos += (pos % 4) switch
                {
                    1 => 3,
                    2 => 2,
                    3 => 1,
                    0 or _ => 0,
                };
                Debug.Assert(pos % 4 == 0);
                var chunkLen = ReadUInt32(in data, pos);
                pos += 4;
                var chunkType = ReadUInt32(in data, pos);
                pos += 4;
                ref readonly var chunkData = ref UnsafeEx.AddByteOffset(in data, pos);
                pos += chunkLen;
                if(chunkType != ChunkType_Bin) { throw new FormatException("Chunk must be 'BIN' (chunk 1)"); }

                var binBuf = glb.CreateNewBuffer(chunkLen);
                unsafe {
                    fixed(void* source = &chunkData) {
                        System.Buffer.MemoryCopy(source, binBuf.Ptr, binBuf.byteLength, chunkLen);
                    }
                }
            }


            // --- Chunk n ---
            while(true) {
                ct.ThrowIfCancellationRequested();
                pos += (pos % 4) switch
                {
                    1 => 3,
                    2 => 2,
                    3 => 1,
                    0 or _ => 0,
                };
                if(pos >= length) {
                    break;
                }
                Debug.Assert(pos % 4 == 0);
                var chunkLen = ReadUInt32(in data, pos);
                pos += 4;
                var chunkType = ReadUInt32(in data, pos);
                pos += 4;
                //ref readonly var chunkData = ref AddOffset(in data, pos);
                ref readonly var chunkData = ref UnsafeEx.AddByteOffset(in data, pos);
                pos += chunkLen;
                if(chunkType == ChunkType_Bin) {
                    var binBuf = glb.CreateNewBuffer(chunkLen);
                    fixed(void* source = &chunkData) {
                        System.Buffer.MemoryCopy(source, binBuf.Ptr, binBuf.byteLength, chunkLen);
                    }
                }
            }

            return glb;
        }
        catch {
            glb?.Dispose();
            throw;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadUInt32(in byte dataHead, nuint offset)
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in dataHead), offset), sizeof(uint)));
    }

    private static void ThrowIfGltfIsNull([NotNull] GltfObject? gltf)
    {
        if(gltf is null) {
            throw new FormatException("glTF is null");
        }
    }

    private record struct GlbHeader(uint Magic, uint Version, uint Length);

    private record struct Chunk(uint ChunkLength, uint ChunkType, uint ChunkData);
}

