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
    public static ITreeModel LoadGlbFile(PbrShader shader, string filePath, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(shader);
        using var glb = GltfParser.ParseGlbFile(filePath, ct);
        var state = new LoaderState
        {
            Glb = glb,
            Shader = shader,
            Ct = ct,
        };
        return LoadRoot(state);
    }

    public static ITreeModel LoadGlb(PbrShader shader, ResourceFile file, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(shader);
        using var glb = GltfParser.ParseGlb(file, ct);
        var state = new LoaderState
        {
            Glb = glb,
            Shader = shader,
            Ct = ct,
        };
        return LoadRoot(state);
    }

    public static ITreeModel LoadGlb(PbrShader shader, ReadOnlySpan<byte> data, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(shader);
        using var glb = GltfParser.ParseGlb(data, ct);
        var state = new LoaderState
        {
            Glb = glb,
            Shader = shader,
            Ct = ct,
        };
        return LoadRoot(state);
    }

    public unsafe static ITreeModel LoadGlb(PbrShader shader, void* data, nuint length, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(shader);
        using var glb = GltfParser.ParseGlb(data, length, ct);
        var state = new LoaderState
        {
            Glb = glb,
            Shader = shader,
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

    private unsafe static ITreeModel LoadNode(in LoaderState state, in Node node)
    {
        state.Ct.ThrowIfCancellationRequested();
        var gltf = state.Gltf;

        ITreeModel model;
        if(node.mesh is uint meshNum) {
            var meshPrimitives = gltf.meshes.GetOrThrow(meshNum).primitives.AsSpan();

            var (vc, ic) = GetMeshInfo(state, meshPrimitives);
            using var verticesBuf = new NativeBuffer(vc * (nuint)Unsafe.SizeOf<Vertex>(), true);
            using var tangentsBuf = new NativeBuffer(vc * (nuint)Unsafe.SizeOf<Vector3>(), true);
            using var indicesBuf = new NativeBuffer(ic * (nuint)Unsafe.SizeOf<uint>(), true);
            var submeshes = new SubmeshData[meshPrimitives.Length];
            var vertices = new SpanU32<Vertex>(verticesBuf.Ptr, vc);
            var tangents = new SpanU32<Vector3>(tangentsBuf.Ptr, vc);
            var indices = new SpanU32<uint>(indicesBuf.Ptr, ic);

            var materials = new Own<Material>[meshPrimitives.Length];
            uint vPos = 0;
            uint iPos = 0;
            for(int i = 0; i < meshPrimitives.Length; i++) {
                var materialData = meshPrimitives[i].material switch
                {
                    uint index => LoadMaterialData(in state, in state.Gltf.materials.GetOrThrow(index)),
                    null => MaterialData.CreateDefault(state.Screen),
                };
                var (vConsumed, iConsumed) = LoadPartMesh(state, meshPrimitives[i], vertices.Slice(vPos), indices.Slice(iPos), tangents.Slice(vPos));
                submeshes[i] = new SubmeshData
                {
                    VertexOffset = (int)vPos,
                    IndexOffset = iPos,
                    IndexCount = iConsumed,
                };
                vPos += vConsumed;
                iPos += iConsumed;
                materials[i] = PbrMaterial.Create(
                    state.Shader,
                    materialData.Pbr.BaseColor,
                    materialData.Pbr.BaseColorSampler,
                    materialData.Pbr.MetallicRoughness,
                    materialData.Pbr.MetallicRoughnessSampler,
                    materialData.Normal.Texture,
                    materialData.Normal.Sampler).Cast<Material>();
            }
            Debug.Assert(vPos == vc);
            Debug.Assert(iPos == ic);
            var mesh = Mesh.Create(state.Screen, new MeshDescriptor<Vertex, uint>
            {
                Indices = new() { Data = indices, Usages = BufferUsages.Index | BufferUsages.CopySrc | BufferUsages.Storage },
                Vertices = new() { Data = vertices, Usages = BufferUsages.Vertex | BufferUsages.CopySrc | BufferUsages.Storage },
                Tangents = new() { Data = tangents, Usages = BufferUsages.Vertex | BufferUsages.CopySrc | BufferUsages.Storage },
                Submeshes = submeshes.AsImmutableArray(),
            });

            model = new FrameObject(mesh, materials.AsImmutableArray())
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

    private static (uint VertexCount, uint IndexCount) GetMeshInfo(in LoaderState state, ReadOnlySpan<MeshPrimitive> meshPrimitives)
    {
        var accessors = state.Gltf.accessors;
        nuint vertexCount = 0;
        nuint indexCount = 0;
        foreach(var meshPrimitive in meshPrimitives) {
            if(meshPrimitive.mode != MeshPrimitiveMode.Triangles) {
                throw new NotImplementedException();
            }
            uint posAttr = meshPrimitive.attributes.POSITION ?? throw new NotSupportedException("no position attribute");
            ref readonly var position = ref accessors.GetOrThrow(posAttr);
            vertexCount += position.count;

            if(meshPrimitive.indices is uint indicesNum) {
                indexCount += accessors.GetOrThrow(indicesNum).count;
            }
            else {
                indexCount += position.count;
            }
        }
        checked {
            return ((uint)vertexCount, (uint)indexCount);
        }
    }

    private unsafe static (Own<Mesh>, Own<PbrMaterial>) LoadMeshAndMaterial<TVertex>(in LoaderState state, in MeshPrimitive meshPrimitive)
        where TVertex : unmanaged, IVertex, IVertexPosition, IVertexUV, IVertexNormal
    {
        return LoadMeshAndMaterial<TVertex, (Own<Mesh>, Own<PbrMaterial>)>(in state, in meshPrimitive, &CreateMeshAndMaterial);

        static (Own<Mesh>, Own<PbrMaterial>) CreateMeshAndMaterial(
            in LoaderState state,
            ReadOnlySpanU32<TVertex> vertices,
            ReadOnlySpanU32<uint> indices,
            ReadOnlySpanU32<Vector3> tangents,
            in MaterialData materialData)
        {
            var mesh = Mesh.CreateWithTangent(state.Screen, vertices, indices, tangents);

            var material = PbrMaterial.Create(
                state.Shader,
                materialData.Pbr.BaseColor,
                materialData.Pbr.BaseColorSampler,
                materialData.Pbr.MetallicRoughness,
                materialData.Pbr.MetallicRoughnessSampler,
                materialData.Normal.Texture,
                materialData.Normal.Sampler);
            return (mesh, material);
        }
    }

    private unsafe static TResult LoadMeshAndMaterial<TVertex, TResult>(
        in LoaderState state,
        in MeshPrimitive meshPrimitive,
        delegate*<in LoaderState, ReadOnlySpanU32<TVertex>, ReadOnlySpanU32<uint>, ReadOnlySpanU32<Vector3>, in MaterialData, TResult> converter)
        where TVertex : unmanaged, IVertex, IVertexPosition, IVertexUV, IVertexNormal
    {
        state.Ct.ThrowIfCancellationRequested();
        var materials = state.Gltf.materials;
        var accessors = state.Gltf.accessors;
        var materialData = meshPrimitive.material switch
        {
            uint index => LoadMaterialData(in state, in materials.GetOrThrow(index)),
            null => MaterialData.CreateDefault(state.Screen),
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
            if(attrs.POSITION is not uint posAttr) {
                throw new NotSupportedException("no position attribute");
            }
            else {
                ref readonly var position = ref accessors.GetOrThrow(posAttr);
                if(position is not { type: AccessorType.Vec3, componentType: AccessorComponentType.Float }) {
                    ThrowHelper.ThrowInvalidGlb();
                }
                var data = AccessData(state, position);
                vertices = new NativeBuffer(data.Count * TVertex.VertexSize, true);
                vertexCount = (uint)data.Count;
                data.CopyToVertexField<TVertex, Vector3>((TVertex*)vertices.Ptr, TVertex.PositionOffset);
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
            else {
                // if not indexed, generate indices
                indices = new NativeBuffer(vertexCount * (nuint)sizeof(uint), false);
                uint* p = (uint*)indices.Ptr;
                for(uint i = 0; i < vertexCount; i++) {
                    p[i] = i;
                }
                indexCount = vertexCount;
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
            else {
                var verticesSpan = new SpanU32<TVertex>(vertices.Ptr, vertexCount);
                var indicesSpan = new SpanU32<uint>(indices.Ptr, indexCount);
                MeshHelper.CalcNormal<TVertex, uint>(verticesSpan, indicesSpan);
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

            var needToCalcTangent =
                //materialData.Normal.Texture.IsNone == false &&
                //attrs.POSITION.HasValue &&
                //attrs.NORMAL.HasValue &&
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

            return converter(
                in state,
                new ReadOnlySpanU32<TVertex>(vertices.Ptr, vertexCount),
                new ReadOnlySpanU32<uint>(indices.Ptr, indexCount),
                new ReadOnlySpanU32<Vector3>(tangents.Ptr, vertexCount),
                in materialData);
        }
        finally {
            vertices.Dispose();
            indices.Dispose();
        }
    }



    private unsafe static (uint VertexConsumedCount, uint IndexConsumedCount) LoadPartMesh<TVertex>(
        in LoaderState state,
        in MeshPrimitive meshPrimitive,
        SpanU32<TVertex> vertices,
        SpanU32<uint> indices,
        SpanU32<Vector3> tangents)
        where TVertex : unmanaged, IVertex, IVertexPosition, IVertexUV, IVertexNormal
    {
        state.Ct.ThrowIfCancellationRequested();
        var materials = state.Gltf.materials;
        var accessors = state.Gltf.accessors;
        var materialData = meshPrimitive.material switch
        {
            uint index => LoadMaterialData(in state, in materials.GetOrThrow(index)),
            null => MaterialData.CreateDefault(state.Screen),
        };

        if(meshPrimitive.mode != MeshPrimitiveMode.Triangles) {
            throw new NotImplementedException();
        }
        ref readonly var attrs = ref meshPrimitive.attributes;
        uint vertexCount;

        // position
        if(attrs.POSITION is not uint posAttr) {
            throw new NotSupportedException("no position attribute");
        }
        else {
            ref readonly var position = ref accessors.GetOrThrow(posAttr);
            if(position is not { type: AccessorType.Vec3, componentType: AccessorComponentType.Float }) {
                ThrowHelper.ThrowInvalidGlb();
            }
            var data = AccessData(state, position);
            vertexCount = (uint)data.Count;
            fixed(TVertex* vp = vertices) {
                data.CopyToVertexField<TVertex, Vector3>(vp, TVertex.PositionOffset);
            }
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
            indexCount = (uint)data.Count;
            fixed(uint* ip = indices) {
                data.StoreIndicesAsUInt32(ip);
            }
        }
        else {
            // if not indexed, generate indices
            for(uint i = 0; i < vertexCount; i++) {
                indices.UnsafeAt(i) = i;
            }
            indexCount = vertexCount;
        }

        // normal
        if(attrs.NORMAL is uint normalAttr) {
            ref readonly var normal = ref accessors.GetOrThrow(normalAttr);
            if(normal is not { type: AccessorType.Vec3, componentType: AccessorComponentType.Float }) {
                ThrowHelper.ThrowInvalidGlb();
            }
            var data = AccessData(state, normal);
            fixed(TVertex* vp = vertices) {
                data.CopyToVertexField<TVertex, Vector3>(vp, TVertex.NormalOffset);
            }
        }
        else {
            MeshHelper.CalcNormal<TVertex, uint>(vertices.Slice(0, vertexCount), indices.Slice(0, indexCount));
        }

        // uv
        if(attrs.TEXCOORD_0 is uint uv0Attr) {
            ref readonly var uv0 = ref accessors.GetOrThrow(uv0Attr);
            if(uv0 is not { type: AccessorType.Vec2, componentType: AccessorComponentType.Float }) {
                ThrowHelper.ThrowInvalidGlb();
            }
            var data = AccessData(state, uv0);
            fixed(TVertex* vp = vertices) {
                data.CopyToVertexField<TVertex, Vector2>(vp, TVertex.UVOffset);
            }
        }

        // tangent
        if(attrs.TANGENT is uint tangentAttr) {
            ref readonly var tangent = ref accessors.GetOrThrow(tangentAttr);
            if(tangent is not { type: AccessorType.Vec4, componentType: AccessorComponentType.Float }) {
                ThrowHelper.ThrowInvalidGlb();
            }
            var data = AccessData(state, tangent);
            fixed(Vector3* vp = tangents) {
                data.CopyToVertexField<Vector3, Vector4, Vector3>(vp, 0, &ConvertData);
            }

            static Vector3 ConvertData(in Vector4 d) => new Vector3(d.X, d.Y, d.Z) * d.W;   // W is -1 or 1 (left-hand or right-hand)
        }
        else {
            if(attrs.TEXCOORD_0 != null) {
                MeshHelper.CalcTangent<TVertex, uint>(vertices, indices, tangents, true);
            }
        }

        return (vertexCount, indexCount);
    }


    private static MaterialData LoadMaterialData(in LoaderState state, in Parsing.Material material)
    {
        var textures = state.Gltf.textures;
        var screen = state.Screen;

        static Own<Sampler> DefaultSampler(Screen screen)
        {
            return Sampler.Create(screen, new SamplerDescriptor
            {
                AddressModeU = AddressMode.ClampToEdge,
                AddressModeV = AddressMode.ClampToEdge,
                AddressModeW = AddressMode.ClampToEdge,
                MagFilter = FilterMode.Linear,
                MinFilter = FilterMode.Linear,
                MipmapFilter = FilterMode.Linear,
            });
        }

        return new MaterialData
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
                        TextureInfo baseColor => LoadTexture(state, textures.GetOrThrow(baseColor.index), TextureFormat.Rgba8UnormSrgb),
                        null => MaterialData.PbrData.DefaultBaseColor(screen),
                    },
                    BaseColorSampler = pbr.baseColorTexture switch
                    {
                        TextureInfo baseColor => LoadSampler(state, textures.GetOrThrow(baseColor.index)),
                        null => DefaultSampler(screen),
                    },
                    MetallicRoughness = pbr.metallicRoughnessTexture switch
                    {
                        TextureInfo metallicRoughness => LoadTexture(state, textures.GetOrThrow(metallicRoughness.index), TextureFormat.Rgba8Unorm),
                        null => MaterialData.PbrData.DefaultMetallicRoughness(screen),
                    },
                    MetallicRoughnessSampler = pbr.metallicRoughnessTexture switch
                    {
                        TextureInfo metallicRoughness => LoadSampler(state, textures.GetOrThrow(metallicRoughness.index)),
                        null => DefaultSampler(screen),
                    },
                },
                null => MaterialData.PbrData.CreateDefault(screen),
            },
            Normal = material.normalTexture switch
            {
                MaterialNormalTextureInfo normal => new()
                {
                    Scale = normal.scale,
                    UVIndex = normal.texCoord,
                    Texture = LoadTexture(state, textures.GetOrThrow(normal.index), TextureFormat.Rgba8Unorm),
                    Sampler = LoadSampler(state, textures.GetOrThrow(normal.index)),
                },
                null => MaterialData.NormalData.CreateDefault(screen),
            },
            Emissive = material.emissiveTexture switch
            {
                TextureInfo emissive => new()
                {
                    Factor = material.emissiveFactor,
                    UVIndex = emissive.texCoord,
                    Texture = LoadTexture(state, textures.GetOrThrow(emissive.index), TextureFormat.Rgba8Unorm),
                    Sampler = LoadSampler(state, textures.GetOrThrow(emissive.index)),
                },
                null => MaterialData.EmissiveData.CreateDefault(screen),
            },
            Occlusion = material.occlusionTexture switch
            {
                MaterialOcclusionTextureInfo occlusion => new()
                {
                    Strength = occlusion.strength,
                    UVIndex = occlusion.texCoord,
                    Texture = LoadTexture(state, textures.GetOrThrow(occlusion.index), TextureFormat.Rgba8Unorm),
                    Sampler = LoadSampler(state, textures.GetOrThrow(occlusion.index)),
                },
                null => MaterialData.OcclusionData.CreateDefault(screen),
            },
        };
    }

    private static Own<Texture2D> LoadTexture(in LoaderState state, in Texture tex, TextureFormat format)
    {
        var gltf = state.Gltf;
        using var image = tex.source switch
        {
            uint index => LoadImage(in state, in gltf.images.GetOrThrow(index)),
            _ => HI.Image.Empty,
        };

        return Texture2D.CreateWithAutoMipmap(state.Screen, image, format, TextureUsages.TextureBinding | TextureUsages.CopySrc);
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
            P = (IntPtr)(bin.Ptr + accessor.byteOffset),
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
        public required CancellationToken Ct { get; init; }
        public required PbrShader Shader { get; init; }

        public Screen Screen => Shader.Screen;

        public readonly GltfObject Gltf => Glb.Gltf;
    }

    private record struct MaterialData
    {
        public required PbrData Pbr;
        public required NormalData Normal;
        public required EmissiveData Emissive;
        public required OcclusionData Occlusion;

        public static MaterialData CreateDefault(Screen screen) => new()
        {
            Pbr = PbrData.CreateDefault(screen),
            Normal = NormalData.CreateDefault(screen),
            Emissive = EmissiveData.CreateDefault(screen),
            Occlusion = OcclusionData.CreateDefault(screen),
        };

        private static Own<Sampler> DefaultSampler(Screen screen)
        {
            return Sampler.Create(screen, new SamplerDescriptor
            {
                AddressModeU = AddressMode.ClampToEdge,
                AddressModeV = AddressMode.ClampToEdge,
                AddressModeW = AddressMode.ClampToEdge,
                MagFilter = FilterMode.Linear,
                MinFilter = FilterMode.Linear,
                MipmapFilter = FilterMode.Linear,
            });
        }

        public record struct PbrData
        {
            public required Vector4 BaseColorFactor;
            public required float MetallicFactor;
            public required float RoughnessFactor;
            public required Own<Texture2D> BaseColor;
            public required Own<Sampler> BaseColorSampler;
            public required Own<Texture2D> MetallicRoughness;
            public required Own<Sampler> MetallicRoughnessSampler;

            public static PbrData CreateDefault(Screen screen) => new()
            {
                BaseColorFactor = DefaultBaseColorFactor(),
                MetallicFactor = DefaultMetallicFactor(),
                RoughnessFactor = DefaultRoughnessFactor(),
                BaseColor = DefaultBaseColor(screen),
                BaseColorSampler = DefaultBaseColorSampler(screen),
                MetallicRoughness = DefaultMetallicRoughness(screen),
                MetallicRoughnessSampler = DefaultMetallicRoughnessSampler(screen),
            };

            public static Vector4 DefaultBaseColorFactor() => Vector4.One;
            public static float DefaultMetallicFactor() => 1.0f;
            public static float DefaultRoughnessFactor() => 1.0f;
            public static Own<Texture2D> DefaultBaseColor(Screen screen) => Texture2D.Create1x1Rgba8UnormSrgb(screen, TextureUsages.TextureBinding | TextureUsages.CopySrc, ColorByte.White);
            public static Own<Sampler> DefaultBaseColorSampler(Screen screen) => DefaultSampler(screen);
            public static Own<Texture2D> DefaultMetallicRoughness(Screen screen) => Texture2D.Create1x1Rgba8Unorm(screen, TextureUsages.TextureBinding | TextureUsages.CopySrc, ColorByte.White);
            public static Own<Sampler> DefaultMetallicRoughnessSampler(Screen screen) => DefaultSampler(screen);
        }

        public record struct NormalData
        {
            public required Own<Texture2D> Texture;
            public required Own<Sampler> Sampler;
            public required float Scale;
            public required uint UVIndex;

            public static NormalData CreateDefault(Screen screen) => new()
            {
                Scale = 1.0f,
                UVIndex = 0,
                Texture = Texture2D.Create1x1Rgba8Unorm(screen, TextureUsages.TextureBinding | TextureUsages.CopySrc, new ColorByte(127, 127, 255, 255)),
                Sampler = DefaultSampler(screen),
            };
        }
        public record struct EmissiveData
        {
            public required Own<Texture2D> Texture;
            public required Own<Sampler> Sampler;
            public required Vector3 Factor;
            public required uint UVIndex;

            public static EmissiveData CreateDefault(Screen screen) => new()
            {
                Factor = Vector3.Zero,
                UVIndex = 0,
                Texture = Texture2D.Create1x1Rgba8Unorm(screen, TextureUsages.TextureBinding | TextureUsages.CopySrc, new ColorByte(0, 0, 0, 0)),
                Sampler = DefaultSampler(screen),
            };
        }

        public record struct OcclusionData
        {
            public required Own<Texture2D> Texture;
            public required Own<Sampler> Sampler;
            public required float Strength;
            public required uint UVIndex;

            public static OcclusionData CreateDefault(Screen screen) => new()
            {
                Strength = 1.0f,
                UVIndex = 0,
                Texture = Texture2D.Create1x1Rgba8Unorm(screen, TextureUsages.TextureBinding | TextureUsages.CopySrc, new ColorByte(0, 0, 0, 0)),
                Sampler = DefaultSampler(screen),
            };
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
