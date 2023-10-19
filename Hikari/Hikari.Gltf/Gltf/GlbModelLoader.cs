#nullable enable
using Hikari.Gltf.Internal;
using Hikari.Gltf.Parsing;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using HI = Hikari.Imaging;

namespace Hikari.Gltf;

public static class GlbModelLoader
{
    public static ITreeModel LoadGlbFile(PbrLayer layer, string filePath, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(layer);
        using var glb = GltfParser.ParseGlbFile(filePath, ct);
        var state = new LoaderState
        {
            Glb = glb,
            Layer = layer,
            Ct = ct,
        };
        return LoadRoot(state);
    }

    public static ITreeModel LoadGlb(PbrLayer layer, ResourceFile file, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(layer);
        using var glb = GltfParser.ParseGlb(file, ct);
        var state = new LoaderState
        {
            Glb = glb,
            Layer = layer,
            Ct = ct,
        };
        return LoadRoot(state);
    }

    public static ITreeModel LoadGlb(PbrLayer layer, ReadOnlySpan<byte> data, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(layer);
        using var glb = GltfParser.ParseGlb(data, ct);
        var state = new LoaderState
        {
            Glb = glb,
            Layer = layer,
            Ct = ct,
        };
        return LoadRoot(state);
    }

    public unsafe static ITreeModel LoadGlb(PbrLayer layer, void* data, nuint length, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(layer);
        using var glb = GltfParser.ParseGlb(data, length, ct);
        var state = new LoaderState
        {
            Glb = glb,
            Layer = layer,
            Ct = ct,
        };
        return LoadRoot(state);
    }

    private static ITreeModel LoadRoot(in LoaderState state)
    {
        var gltf = state.Gltf;
        if(gltf.asset.version != "2.0"u8) {
            throw new NotSupportedException("only supports gltf v2.0");
        }

        var root = new EmptyTreeModel();
        if(gltf.scene is uint sceneNum) {
            ref readonly var scene = ref gltf.scenes.GetOrThrow(sceneNum);
            foreach(var nodeNum in scene.nodes.AsSpan()) {
                var child = LoadNode(in state, in gltf.nodes.GetOrThrow(nodeNum));
                root.AddChild(child);
            }
        }
        return root;
    }

    private static ITreeModel LoadNode(in LoaderState state, in Node node)
    {
        var gltf = state.Gltf;

        ITreeModel model;
        if(node.mesh is uint meshNum) {
            var meshPrimitives = gltf.meshes.GetOrThrow(meshNum).primitives.AsSpan();
            if(meshPrimitives.Length != 1) {
                throw new NotImplementedException();        // TODO:
            }
            //for(int i = 0; i < meshPrimitives.Length; i++) {
            //    var (mesh, material) = ReadMeshPrimitive<Vertex>(in state, in meshPrimitives[i]);
            //}
            var (mesh, material) = LoadMeshAndMaterial<Vertex>(in state, in meshPrimitives[0]);
            model = new PbrModel(mesh, material)
            {
                Name = node.name?.ToString(),
            };
        }
        else {
            model = new EmptyTreeModel()
            {
                Name = node.name?.ToString(),
            };
        }

        // glTF and Engine has same coordinate (Y-up, right-hand)
        model.Rotation = new Quaternion(node.rotation.X, node.rotation.Y, node.rotation.Z, node.rotation.W);
        model.Position = new Vector3(node.translation.X, node.translation.Y, node.translation.Z);
        model.Scale = new Vector3(node.scale.X, node.scale.Y, node.scale.Z);
        var matrix = new Matrix4(node.matrix.AsSpan()); // TODO:

        foreach(var childNum in node.children.AsSpan()) {
            var child = LoadNode(in state, in gltf.nodes.GetOrThrow(childNum));
            model.AddChild(child);
        }
        return model;
    }

    private unsafe static (Own<Mesh<TVertex>>, Own<PbrMaterial>) LoadMeshAndMaterial<TVertex>(in LoaderState state, in MeshPrimitive meshPrimitive)
        where TVertex : unmanaged, IVertex, IVertexPosition, IVertexUV, IVertexNormal
    {
        var materials = state.Gltf.materials;
        var accessors = state.Gltf.accessors;
        var material = meshPrimitive.material switch
        {
            uint index => LoadMaterial(in state, in materials.GetOrThrow(index)),
            null => Own<PbrMaterial>.None,
        };
        if(meshPrimitive.mode != MeshPrimitiveMode.Triangles) {
            throw new NotImplementedException();
        }
        ref readonly var attrs = ref meshPrimitive.attributes;
        var vertices = NativeBuffer.Empty;
        var indices = NativeBuffer.Empty;
        uint vertexCount;
        try {
            // position
            if(attrs.POSITION is uint posAttr) {
                ref readonly var position = ref accessors.GetOrThrow(posAttr);
                if(position is not { type: AccessorType.Vec3, componentType: AccessorComponentType.Float }) {
                    ThrowHelper.ThrowInvalidGlb();
                }
                var data = AccessData(state, position);
                vertices = new NativeBuffer(data.Count * TVertex.VertexSize, true);
                vertexCount = (uint)data.Count;
                data.CopyToVertexField<TVertex, Vector3>((TVertex*)vertices.Ptr, TVertex.PositionOffset);
            }
            else {
                throw new NotSupportedException();
            }


            // normal
            if(attrs.NORMAL is uint normalAttr) {
                ref readonly var normal = ref accessors.GetOrThrow(normalAttr);
                if(normal is not { type: AccessorType.Vec3, componentType: AccessorComponentType.Float }) {
                    ThrowHelper.ThrowInvalidGlb();
                }
                var data = AccessData(state, normal);
                data.CopyToVertexField<TVertex, Vector3>((TVertex*)vertices.Ptr, TVertex.NormalOffset);
            }

            // uv
            if(attrs.TEXCOORD_0 is uint uv0Attr) {
                ref readonly var uv0 = ref accessors.GetOrThrow(uv0Attr);
                if(uv0 is not { type: AccessorType.Vec2, componentType: AccessorComponentType.Float }) {
                    ThrowHelper.ThrowInvalidGlb();
                }
                var data = AccessData(state, uv0);
                data.CopyToVertexField<TVertex, Vector2>((TVertex*)vertices.Ptr, TVertex.UVOffset);
            }

            // tangent
            using var tangents = new NativeBuffer(vertexCount * (nuint)sizeof(Vector3), true);
            if(attrs.TANGENT is uint tangentAttr) {
                ref readonly var tangent = ref accessors.GetOrThrow(tangentAttr);
                if(tangent is not { type: AccessorType.Vec4, componentType: AccessorComponentType.Float }) {
                    ThrowHelper.ThrowInvalidGlb();
                }
                var data = AccessData(state, tangent);
                data.CopyToVertexField<Vector3, Vector4, Vector3>((Vector3*)tangents.Ptr, 0, &ConvertData);

                static Vector3 ConvertData(in Vector4 d) => new Vector3(d.X, d.Y, d.Z) * d.W;   // W is -1 or 1 (left-hand or right-hand)
            }

            // indices
            var indexCount = 0u;
            if(meshPrimitive.indices is uint indicesNum) {
                ref readonly var indexAccessor = ref accessors.GetOrThrow(indicesNum);
                if(indexAccessor is not
                    {
                        type: AccessorType.Scalar,
                        componentType: AccessorComponentType.UnsignedByte or AccessorComponentType.UnsignedShort or AccessorComponentType.UnsignedInt
                    }) {
                    ThrowHelper.ThrowInvalidGlb();
                }
                var data = AccessData(state, indexAccessor);
                indices = new NativeBuffer(data.Count * (nuint)sizeof(uint), false);
                indexCount = (uint)data.Count;
                data.StoreIndicesAsUInt32((uint*)indices.Ptr);
            }

            var needToCalcTangent =
                attrs.POSITION.HasValue &&
                attrs.NORMAL.HasValue &&
                attrs.TEXCOORD_0.HasValue &&
                attrs.TANGENT.HasValue == false;
            if(needToCalcTangent) {
                if(indices.ByteLength == 0) {
                    VertexHelper.CalcTangentsSeparated((TVertex*)vertices.Ptr, vertexCount, (Vector3*)tangents.Ptr, vertexCount);
                }
                else {
                    VertexHelper.CalcTangentsSeparatedIndexed((TVertex*)vertices.Ptr, vertexCount, (Vector3*)tangents.Ptr, vertexCount, (uint*)indices.Ptr, indexCount, true);
                }
            }

            var mesh = Mesh.Create(state.Screen, (TVertex*)vertices.Ptr, vertexCount, (uint*)indices.Ptr, indexCount, (Vector3*)tangents.Ptr, vertexCount);
            return (mesh, material);
        }
        finally {
            vertices.Dispose();
            indices.Dispose();
        }
    }

    private static Own<PbrMaterial> LoadMaterial(in LoaderState state, in Material material)
    {
        var textures = state.Gltf.textures;
        var matData = new MaterialData
        {
            Pbr = material.pbrMetallicRoughness switch
            {
                MaterialPbrMetallicRoughness pbr => new()
                {
                    MetallicFactor = pbr.metallicFactor,
                    BaseColorFactor = new Vector4(pbr.baseColorFactor.X, pbr.baseColorFactor.Y, pbr.baseColorFactor.Z, pbr.baseColorFactor.W),
                    RoughnessFactor = pbr.roughnessFactor,
                    BaseColor = pbr.baseColorTexture switch
                    {
                        TextureInfo baseColor => LoadTexture(state, textures.GetOrThrow(baseColor.index)),
                        null => Own<Texture2D>.None,
                    },
                    BaseColorSampler = pbr.baseColorTexture switch
                    {
                        TextureInfo baseColor => LoadSampler(state, textures.GetOrThrow(baseColor.index)),
                        null => Own<Sampler>.None,
                    },
                    MetallicRoughness = pbr.metallicRoughnessTexture switch
                    {
                        TextureInfo metallicRoughness => LoadTexture(state, textures.GetOrThrow(metallicRoughness.index)),
                        null => Own<Texture2D>.None,
                    },
                    MetallicRoughnessSampler = pbr.metallicRoughnessTexture switch
                    {
                        TextureInfo metallicRoughness => LoadSampler(state, textures.GetOrThrow(metallicRoughness.index)),
                        null => Own<Sampler>.None,
                    },
                },
                null => null,
            },
            Normal = material.normalTexture switch
            {
                MaterialNormalTextureInfo normal => new()
                {
                    Scale = normal.scale,
                    UVIndex = normal.texCoord,
                    Texture = LoadTexture(state, textures.GetOrThrow(normal.index)),
                    Sampler = LoadSampler(state, textures.GetOrThrow(normal.index)),
                },
                null => null,
            },
            Emissive = material.emissiveTexture switch
            {
                TextureInfo emissive => new()
                {
                    Factor = material.emissiveFactor,
                    UVIndex = emissive.texCoord,
                    Texture = LoadTexture(state, textures.GetOrThrow(emissive.index)),
                    Sampler = LoadSampler(state, textures.GetOrThrow(emissive.index)),
                },
                null => null,
            },
            Occlusion = material.occlusionTexture switch
            {
                MaterialOcclusionTextureInfo occlusion => new()
                {
                    Strength = occlusion.strength,
                    UVIndex = occlusion.texCoord,
                    Texture = LoadTexture(state, textures.GetOrThrow(occlusion.index)),
                    Sampler = LoadSampler(state, textures.GetOrThrow(occlusion.index)),
                },
                null => null,
            },
        };
        return PbrMaterial.Create(
            state.Layer.GetDefaultShader(),
            matData.Pbr!.Value.BaseColor,
            matData.Pbr!.Value.BaseColorSampler,
            matData.Pbr!.Value.MetallicRoughness,
            matData.Pbr!.Value.MetallicRoughnessSampler,
            matData.Normal!.Value.Texture,
            matData.Normal!.Value.Sampler);
    }

    private static Own<Texture2D> LoadTexture(in LoaderState state, in Texture tex)
    {
        var gltf = state.Gltf;
        using var image = tex.source switch
        {
            uint index => LoadImage(in state, in gltf.images.GetOrThrow(index)),
            _ => default,
        };

        return Texture2D.CreateWithAutoMipmap(state.Screen, image, TextureFormat.Rgba8UnormSrgb, TextureUsages.CopySrc | TextureUsages.TextureBinding);
    }

    private static Own<Sampler> LoadSampler(in LoaderState state, in Texture tex)
    {
        var gltf = state.Gltf;
        switch(tex.sampler) {
            case uint index: {
                ref readonly var s = ref gltf.samplers.GetOrThrow(index);
                return Sampler.Create(state.Screen, new SamplerDescriptor
                {
                    AddressModeU = GetAddressMode(s.wrapS),
                    AddressModeV = GetAddressMode(s.wrapT),
                    AddressModeW = AddressMode.ClampToEdge,
                    MagFilter = GetMagFilterMode(s.magFilter),
                    MinFilter = GetMinFilterMode(s.minFilter, out var mipmapFilter),
                    MipmapFilter = mipmapFilter,
                });
            }
            default: {
                return Sampler.Create(state.Screen, new SamplerDescriptor
                {
                    AddressModeU = AddressMode.ClampToEdge,
                    AddressModeV = AddressMode.ClampToEdge,
                    AddressModeW = AddressMode.ClampToEdge,
                    MagFilter = FilterMode.Linear,
                    MinFilter = FilterMode.Linear,
                    MipmapFilter = FilterMode.Linear,
                });
            }
        }

        static AddressMode GetAddressMode(SamplerWrap wrap)
        {
            return wrap switch
            {
                SamplerWrap.Repeat => AddressMode.Repeat,
                SamplerWrap.MirroredRepeat => AddressMode.MirrorRepeat,
                SamplerWrap.ClampToEdge => AddressMode.ClampToEdge,
                _ => AddressMode.ClampToEdge,
            };
        }

        static FilterMode GetMagFilterMode(SamplerMagFilter? value)
        {
            return value switch
            {
                SamplerMagFilter.Nearest => FilterMode.Nearest,
                SamplerMagFilter.Linear => FilterMode.Linear,
                null => FilterMode.Linear,
                _ => FilterMode.Linear,
            };
        }

        static FilterMode GetMinFilterMode(SamplerMinFilter? value, out FilterMode mipmapFilterMode)
        {
            (var minFilterMode, mipmapFilterMode) = value switch
            {
                SamplerMinFilter.Nearest => (FilterMode.Nearest, FilterMode.Nearest),
                SamplerMinFilter.Linear => (FilterMode.Linear, FilterMode.Nearest),
                SamplerMinFilter.NearestMipmapNearest => (FilterMode.Nearest, FilterMode.Nearest),
                SamplerMinFilter.LinearMipmapNearest => (FilterMode.Linear, FilterMode.Nearest),
                SamplerMinFilter.NearestMipmapLinear => (FilterMode.Nearest, FilterMode.Linear),
                SamplerMinFilter.LinearMipmapLinear => (FilterMode.Linear, FilterMode.Linear),
                null or _ => (FilterMode.Linear, FilterMode.Linear),
            };
            return minFilterMode;
        }
    }

    private enum BufferWriteDestinationMode
    {
        AllocateNew,
        AllocateNewWithoutInit,
        ExistingMemory,
    }

    private unsafe static BufferData AccessData(in LoaderState state, in Accessor accessor)
    {
        var gltf = state.Gltf;
        if(accessor.bufferView is uint bufferViewNum == false) {
            throw new InvalidOperationException("bufferView is not specified in accessor.");
        }
        ref readonly var bufferView = ref gltf.bufferViews.GetOrThrow(bufferViewNum);
        var bin = ReadBufferView(in state, in bufferView);
        return new BufferData
        {
            P = (IntPtr)bin.Ptr,
            ByteLength = bin.ByteLength,
            ByteStride = bufferView.byteStride,
            Count = accessor.count,
            Type = accessor.type,
            ComponentType = accessor.componentType,
        };
    }

    private unsafe static GlbBinaryData ReadBufferView(in LoaderState state, in BufferView bufferView)
    {
        var gltf = state.Gltf;
        ref readonly var buffer = ref gltf.buffers.GetOrThrow(bufferView.buffer);
        if(buffer.uri == null) {
            nuint offset = bufferView.byteOffset;
            nuint len = bufferView.byteLength;
            var bin = state.Glb.GetBinaryData(bufferView.buffer).Slice(offset, len);
            return bin;
        }
        else {
            ThrowHelper.ThrowUriNotSupported();
            return default;
        }
    }

    private unsafe static HI.Image LoadImage(in LoaderState state, in Image image)
    {
        var gltf = state.Gltf;
        if(image.uri != null) {
            ThrowHelper.ThrowUriNotSupported();
        }

        if(image.bufferView.TryGetValue(out var bufferViewNum) == false) {
            ThrowHelper.ThrowInvalidGlb();
        }
        ref readonly var bufferView = ref gltf.bufferViews.GetOrThrow(bufferViewNum);
        var bin = ReadBufferView(in state, in bufferView);
        if(image.mimeType.TryGetValue(out var mimeType) == false) {
            ThrowHelper.ThrowInvalidGlb();
        }
        using var stream = new PointerMemoryStream(bin.Ptr, bin.ByteLength);

        return mimeType switch
        {
            ImageMimeType.ImageJpeg => HI.Image.FromStream(stream, HI.ImageType.Jpg),
            ImageMimeType.ImagePng => HI.Image.FromStream(stream, HI.ImageType.Png),
            _ => default,
        };
    }

    private record struct BufferData
    {
        public required IntPtr P;
        public required nuint ByteLength;
        public required nuint? ByteStride;
        public required nuint Count;
        public required AccessorType Type;
        public required AccessorComponentType ComponentType;

        public readonly unsafe void* Ptr => (void*)P;

        public readonly unsafe void CopyToVertexField<TVertex, TField>(TVertex* vertices, uint fieldOffset)
            where TVertex : unmanaged
            where TField : unmanaged
        {
            if(BitConverter.IsLittleEndian == false) {
                throw new PlatformNotSupportedException("Big endian environment is not supported.");
            }
            var ptr = (byte*)P;
            var byteStride = ByteStride ?? (nuint)sizeof(TField);
            for(nuint i = 0; i < Count; i++) {
                VertexAccessor.GetRefField<TVertex, TField>(ref vertices[i], fieldOffset) = *(TField*)(ptr + byteStride * i);
            }
        }

        public readonly unsafe void CopyToVertexField<TVertex, TData, TField>(TVertex* vertices, uint fieldOffset, delegate*<in TData, TField> map)
            where TVertex : unmanaged
            where TData : unmanaged
            where TField : unmanaged
        {
            if(BitConverter.IsLittleEndian == false) {
                throw new PlatformNotSupportedException("Big endian environment is not supported.");
            }
            var ptr = (byte*)P;
            var byteStride = ByteStride ?? (nuint)sizeof(TField);
            for(nuint i = 0; i < Count; i++) {
                VertexAccessor.GetRefField<TVertex, TField>(ref vertices[i], fieldOffset) = *(TField*)(ptr + byteStride * i);
            }
        }

        public readonly unsafe void StoreIndicesAsUInt32(uint* dest)
        {
            var elementCount = Count;
            switch(ComponentType) {
                case AccessorComponentType.UnsignedByte: {
                    PrimitiveHelper.ConvertUInt8ToUInt32((byte*)Ptr, dest, elementCount);
                    break;
                }
                case AccessorComponentType.UnsignedShort: {
                    PrimitiveHelper.ConvertUInt16ToUInt32((ushort*)Ptr, dest, elementCount);
                    break;
                }
                case AccessorComponentType.UnsignedInt: {
                    System.Buffer.MemoryCopy(Ptr, dest, ByteLength, ByteLength);
                    if(BitConverter.IsLittleEndian == false) {
                        PrimitiveHelper.ReverseEndianUInt32(dest, elementCount);
                    }
                    break;
                }
                default: {
                    Debug.Fail("It should not be possible to reach here.");
                    break;
                }

            }
        }
    }

    private readonly record struct LoaderState
    {
        public required GlbObject Glb { get; init; }
        public required PbrLayer Layer { get; init; }
        public required CancellationToken Ct { get; init; }

        public Screen Screen => Layer.Screen;

        public readonly GltfObject Gltf => Glb.Gltf;
    }

    private record struct MaterialData
    {
        public required PbrData? Pbr;
        public required NormalData? Normal;
        public required EmissiveData? Emissive;
        public required OcclusionData? Occlusion;

        public record struct PbrData
        {
            public required Vector4 BaseColorFactor;
            public required float MetallicFactor;
            public required float RoughnessFactor;
            public required Own<Texture2D> BaseColor;
            public required Own<Sampler> BaseColorSampler;
            public required Own<Texture2D> MetallicRoughness;
            public required Own<Sampler> MetallicRoughnessSampler;
        }

        public record struct NormalData
        {
            public required Own<Texture2D> Texture;
            public required Own<Sampler> Sampler;
            public required float Scale;
            public required uint UVIndex;
        }
        public record struct EmissiveData
        {
            public required Own<Texture2D> Texture;
            public required Own<Sampler> Sampler;
            public required Vector3 Factor;
            public required uint UVIndex;
        }

        public record struct OcclusionData
        {
            public required Own<Texture2D> Texture;
            public required Own<Sampler> Sampler;
            public required float Strength;
            public required uint UVIndex;
        }
    }
}

file static class LocalExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetOrThrow<T>(this T[]? array, uint index)
    {
        if(array == null) {
            ThrowHelper.ThrowFormat(ErrorMessage.InvalidGlb);
        }
        return ref array[index];
    }
}

file static class PrimitiveHelper
{
    public static unsafe void ConvertUInt16ToUInt32(ushort* src, uint* dest, nuint elementCount)
    {
        if(Sse2.IsSupported && Avx2.IsSupported) {
            // extend each packed u16 to u32
            //
            // <u16, u16, u16, u16, u16, u16, u16, u16> (128 bits)
            //   |    |    |    |    |    |    |    |
            //   |    |    |    |    |    |    |    | 
            // <u32, u32, u32, u32, u32, u32, u32, u32> (256 bits)

            var (n, m) = Math.DivRem(elementCount, 8);

            const uint LoopUnrollFactor = 4;
            var (n1, n2) = Math.DivRem(n, LoopUnrollFactor);
            for(nuint i = 0; i < n1; i++) {
                var x = i * 8 * LoopUnrollFactor;
                Unsafe.As<uint, Vector256<int>>(ref dest[x]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x]));
                Unsafe.As<uint, Vector256<int>>(ref dest[x + 8]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x + 8]));
                Unsafe.As<uint, Vector256<int>>(ref dest[x + 16]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x + 16]));
                Unsafe.As<uint, Vector256<int>>(ref dest[x + 24]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x + 24]));
            }
            var offset = n1 * 8 * LoopUnrollFactor;
            for(nuint i = 0; i < n2; i++) {
                var x = offset + i * 8;
                Unsafe.As<uint, Vector256<int>>(ref dest[x]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x]));
            }
            offset += n2 * 8;
            for(nuint i = 0; i < m; i++) {
                dest[offset + i] = (uint)src[offset + i];
            }
        }
        else {
            NonVectorFallback(src, dest, elementCount);
        }

        static void NonVectorFallback(ushort* src, uint* dest, nuint elementCount)
        {
            for(nuint i = 0; i < elementCount; i++) {
                dest[i] = (uint)src[i];
            }
        }
    }

    public static unsafe void ConvertUInt8ToUInt32(byte* src, uint* dest, nuint elementCount)
    {
        if(Sse2.IsSupported && Avx2.IsSupported) {
            // extend each packed u8 to u32
            // 
            // (uint8 * 16) is packed in 128 bits,
            // but 'Avx2.ConvertToVector256Int32' method converts only eight packed uint8 in lower 64 bits.

            // 128 bits
            // <u8, u8, u8, u8, u8, u8, u8, u8, u8, u8, u8, u8, u8, u8, u8, u8>
            //                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //                                              | lower 64 bits
            // 256 bits                                     |
            // <u32, u32, u32, u32, u32, u32, u32, u32>  <--'

            var (n, m) = Math.DivRem(elementCount, 8);

            const uint LoopUnrollFactor = 4;
            var (n1, n2) = Math.DivRem(n, LoopUnrollFactor);
            for(nuint i = 0; i < n1; i++) {
                var x = i * 8 * LoopUnrollFactor;
                Unsafe.As<uint, Vector256<int>>(ref dest[x]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x]));
                Unsafe.As<uint, Vector256<int>>(ref dest[x + 8]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x + 8]));
                Unsafe.As<uint, Vector256<int>>(ref dest[x + 16]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x + 16]));
                Unsafe.As<uint, Vector256<int>>(ref dest[x + 24]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x + 24]));
            }
            var offset = n1 * 8 * LoopUnrollFactor;
            for(nuint i = 0; i < n2; i++) {
                var x = offset + i * 8;
                Unsafe.As<uint, Vector256<int>>(ref dest[x]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x]));
            }
            offset += n2 * 8;
            for(nuint i = 0; i < m; i++) {
                dest[offset + i] = (uint)src[offset + i];
            }
        }
        else {
            NonVectorFallback(src, dest, elementCount);
        }

        static void NonVectorFallback(byte* src, uint* dest, nuint elementCount)
        {
            for(nuint i = 0; i < elementCount; i++) {
                dest[i] = (uint)src[i];
            }
        }
    }

    public unsafe static void ReverseEndianUInt32(uint* p, nuint count)
    {
        for(nuint i = 0; i < count; i++) {
            p[i] = ((p[i] & 0x0000_00FF) << 24) + ((p[i] & 0x0000_FF00) << 8) + ((p[i] & 0x00FF_0000) >> 8) + ((p[i] & 0xFF00_0000) >> 24);
        }
    }
}

internal static class ThrowHelper
{
    [DoesNotReturn]
    [DebuggerHidden]
    public static void ThrowFormat(string message) => throw new FormatException(message);

    [DoesNotReturn]
    [DebuggerHidden]
    public static void ThrowInvalidGlb() => throw new FormatException("invalid glb");

    [DoesNotReturn]
    [DebuggerHidden]
    public static void ThrowUriNotSupported() => throw new NotSupportedException("Data from URI is not supported.");
}

internal static class ErrorMessage
{
    public const string InvalidGlb = "invalid glb";
}
