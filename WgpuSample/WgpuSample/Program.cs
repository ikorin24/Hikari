#nullable enable
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Elffy.Bind;

namespace Elffy;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Environment.SetEnvironmentVariable("RUST_BACKTRACE", "1", EnvironmentVariableTarget.Process);
        EngineCore.EngineStart(new()
        {
            OnStart = OnStart,
            OnRender = OnRender,
        });
    }

    private static State _state;
    const int NUM_INSTANCES_PER_ROW = 10;
    private static readonly Vector3 INSTANCE_DISPLACEMENT = new(
        NUM_INSTANCES_PER_ROW * 0.5f, 0, NUM_INSTANCES_PER_ROW * 0.5f
        );

    private static unsafe void OnStart(HostScreenHandle screen, in HostScreenInfo info)
    {
        var surfaceFormat = info.surface_format.Unwrap();
        System.Diagnostics.Debug.WriteLine(info.backend);


        // Texture
        var diffuseTexture = HostScreenInitializer.CreateTexture(screen, "happy-tree.png");

        // TextureView, Sampler
        var (textureView, sampler) = HostScreenInitializer.CreateTextureViewSampler(screen, diffuseTexture);

        // BindGroupLayout
        var textureBindGroupLayout = HostScreenInitializer.CreateTextureBindGroupLayout(screen);

        // BindGroup
        var diffuseBindGroup = HostScreenInitializer.CreateTextureBindGroup(screen, textureBindGroupLayout, textureView, sampler);

        var camera = new Camera
        {
            eye = new(0.0f, 5.0f, -10.0f),
            target = new(0, 0, 0),
            up = new(0, 1, 0),
            aspect = 1280f / 720f,  // width / height
            fovy = 45f / 180f * float.Pi,
            znear = 0.1f,
            zfar = 100f,
        };
        var cameraUniform = CameraUniform.Default;
        cameraUniform.UpdateViewProj(camera);

        var cameraBuffer = EngineCore.CreateBufferInit(
            screen,
            new Slice<byte>(&cameraUniform, sizeof(CameraUniform)),
            wgpu_BufferUsages.UNIFORM | wgpu_BufferUsages.COPY_DST);

        var instances = Enumerable
            .Range(0, NUM_INSTANCES_PER_ROW)
            .SelectMany(z => Enumerable.Range(0, NUM_INSTANCES_PER_ROW).Select(x =>
            {
                var position = new Vector3(x, 0, z) - INSTANCE_DISPLACEMENT;
                var rotation = position.IsZero ?
                    Quaternion.FromAxisAngle(Vector3.UnitZ, 0) :
                    Quaternion.FromAxisAngle(position.Normalized(), 45f / 180f * float.Pi);
                var model = rotation.ToMatrix4() * Matrix4.FromScaleAndTranslation(Vector3.One, position);
                //var model = Matrix4.Identity;
                return new InstanceRaw
                {
                    Model = model,
                };
            }))
            .ToArray();

        // Buffer (instance)
        BufferHandle instanceBuffer;
        int instanceCount = instances.Length;
        fixed(void* instanceData = instances) {
            instanceBuffer = EngineCore.CreateBufferInit(
                screen,
                new Slice<byte>(instanceData, sizeof(InstanceRaw) * instances.Length),
                wgpu_BufferUsages.VERTEX | wgpu_BufferUsages.COPY_DST);
        }

        var cameraBindGroupLayout = EngineCore.CreateBindGroupLayout(screen, UnsafeEx.StackPointer(new BindGroupLayoutDescriptor
        {
            entries = Slice.FromFixedSpanUnsafe(stackalloc BindGroupLayoutEntry[1]
            {
                new()
                {
                    binding = 0,
                    visibility = wgpu_ShaderStages.VERTEX,
                    ty = BindingType.Buffer(UnsafeEx.StackPointer(new BufferBindingData
                    {
                        ty = BufferBindingType.Uniform,
                        has_dynamic_offset = false,
                        min_binding_size = 0,
                    })),
                    count = 0,
                },
            }),
        }));

        BindGroupHandle cameraBindGroup;
        {
            var bufferBinding = cameraBuffer.AsEntireBufferBinding();
            Span<BindGroupEntry> entries = stackalloc BindGroupEntry[]
            {
                new() { binding = 0, resource = BindingResource.Buffer(&bufferBinding), }
            };
            var desc = new BindGroupDescriptor
            {
                layout = cameraBindGroupLayout,
                entries = new Slice<BindGroupEntry>(Unsafe.AsPointer(ref entries[0]), entries.Length),
            };
            cameraBindGroup = EngineCore.CreateBindGroup(screen, &desc);
        }

        var shader = EngineCore.CreateShaderModule(screen, ShaderSource);

        //// BindGroupLayout (uniform)
        //var uniformBindGroupLayout = HostScreenInitializer.CreateUniformBindGroupLayout(screen);

        // PipelineLayout
        var pipelineLayout = EngineCore.CreatePipelineLayout(screen, UnsafeEx.StackPointer(new PipelineLayoutDescriptor
        {
            bind_group_layouts = Slice.FromFixedSpanUnsafe(stackalloc BindGroupLayoutHandle[]
            {
                textureBindGroupLayout,
                cameraBindGroupLayout,
            }),
        }));

        //// Buffer (uniform)
        //var uniformBuffer = HostScreenInitializer.CreateUniformBuffer(screen, stackalloc Vector4[] { new Vector4(1, 0, 0, 1) });

        //// BindGroup (uniform)
        //var uniformBindGroup = HostScreenInitializer.CreateUniformBindGroup(screen, uniformBindGroupLayout, uniformBuffer);

        // RenderPipeline
        RenderPipelineHandle renderPipeline;
        {
            var vertexBufferLayout = new VertexBufferLayout
            {
                array_stride = (ulong)sizeof(Vertex),
                step_mode = wgpu_VertexStepMode.Vertex,
                attributes = Slice.FromFixedSpanUnsafe(stackalloc wgpu_VertexAttribute[2]
                {
                    new() { offset = 0, shader_location = 0, format = wgpu_VertexFormat.Float32x3 },
                    new() { offset = 12, shader_location = 1, format = wgpu_VertexFormat.Float32x2 },
                }),
            };
            var instanceBufferLayout = new VertexBufferLayout
            {
                array_stride = (ulong)sizeof(InstanceRaw),
                step_mode = wgpu_VertexStepMode.Instance,
                attributes = Slice.FromFixedSpanUnsafe(stackalloc wgpu_VertexAttribute[]
                {
                    new() { offset = 4 * 0, shader_location = 5, format = wgpu_VertexFormat.Float32x4 },
                    new() { offset = 4 * 4, shader_location = 6, format = wgpu_VertexFormat.Float32x4 },
                    new() { offset = 4 * 8, shader_location = 7, format = wgpu_VertexFormat.Float32x4 },
                    new() { offset = 4 * 12, shader_location = 8, format = wgpu_VertexFormat.Float32x4 },
                }),
            };

            var desc = new RenderPipelineDescriptor
            {
                layout = pipelineLayout,
                vertex = new()
                {
                    module = shader,
                    entry_point = Slice.FromFixedSpanUnsafe("vs_main"u8),
                    buffers = Slice.FromFixedSpanUnsafe(stackalloc VertexBufferLayout[]
                    {
                        vertexBufferLayout,
                        instanceBufferLayout,
                    }),
                },
                fragment = Opt.Some(new FragmentState
                {
                    module = shader,
                    entry_point = Slice.FromFixedSpanUnsafe("fs_main"u8),
                    targets = Slice.FromFixedSpanUnsafe(stackalloc Opt<ColorTargetState>[]
                    {
                        Opt.Some(new ColorTargetState
                        {
                            format = surfaceFormat,
                            blend = Opt.Some(wgpu_BlendState.REPLACE),
                            write_mask = wgpu_ColorWrites.ALL,
                        })
                    }),
                }),
                primitive = new()
                {
                    topology = wgpu_PrimitiveTopology.TriangleList,
                    strip_index_format = Opt.None<wgpu_IndexFormat>(),
                    front_face = wgpu_FrontFace.Ccw,
                    cull_mode = Opt.Some(wgpu_Face.Back),
                    polygon_mode = wgpu_PolygonMode.Fill,
                },
            };
            renderPipeline = EngineCore.CreateRenderPipeline(screen, &desc);
        }

        // Buffer (vertex, index)
        BufferHandle vertexBuffer;
        BufferHandle indexBuffer;
        int indexCount;
        wgpu_IndexFormat indexFormat;
        {
            var (vertices, indices) = SamplePrimitives.SampleData();
            indexCount = indices.Length;
            fixed(void* vData = vertices) {
                vertexBuffer = EngineCore.CreateBufferInit(
                    screen,
                    new Slice<byte>(vData, sizeof(Vertex) * vertices.Length),
                    wgpu_BufferUsages.VERTEX);
            }
            fixed(void* iData = indices) {
                indexBuffer = EngineCore.CreateBufferInit(
                    screen,
                    new Slice<byte>(iData, sizeof(ushort) * indices.Length),
                    wgpu_BufferUsages.INDEX);
            }
            indexFormat = wgpu_IndexFormat.Uint16;
        }



        _state = new State
        {
            PipelineLayout = pipelineLayout,
            Shader = shader,
            RenderPipeline = renderPipeline,
            VertexBuffer = vertexBuffer,
            //VertexCount = vertexCount,
            IndexBuffer = indexBuffer,
            IndexCount = indexCount,
            IndexFormat = indexFormat,
            InstanceBuffer = instanceBuffer,
            InstanceCount = instanceCount,
            DiffuseTexture = diffuseTexture,
            TextureView = textureView,
            Sampler = sampler,
            TextureBindGroupLayout = textureBindGroupLayout,
            DiffuseBindGroup = diffuseBindGroup,

            CameraBuffer = cameraBuffer,
            CameraBindGroupLayout = cameraBindGroupLayout,
            CameraBindGroup = cameraBindGroup,
            //UniformBuffer = uniformBuffer,
            //UniformBindGroupLayout = uniformBindGroupLayout,
            //UniformBindGroup = uniformBindGroup,
        };
    }

    ////[StructLayout(LayoutKind.Sequential, Size = 4)]
    //private struct InstanceData
    //{
    //    public Vector3 Value;
    //}

    private static unsafe void OnRender(HostScreenHandle screen, RenderPassRef renderPass)
    {
        //var color = new Vector4
        //{
        //    X = Random.Shared.NextSingle(),
        //    Y = 0,
        //    Z = 0,
        //    W = 0,
        //};
        //var data = new Slice<byte>(&color, (nuint)sizeof(Vector4));
        //EngineCore.WriteBuffer(screen, _state.UniformBuffer, 0, data);

        renderPass.SetPipeline(_state.RenderPipeline);
        renderPass.SetBindGroup(0, _state.DiffuseBindGroup);
        renderPass.SetBindGroup(1, _state.CameraBindGroup);

        renderPass.SetVertexBuffer(0, new BufSlice(_state.VertexBuffer, RangeBoundsU64.All));
        renderPass.SetVertexBuffer(1, new BufSlice(_state.InstanceBuffer, RangeBoundsU64.All));
        renderPass.SetIndexBuffer(new BufSlice(_state.IndexBuffer, RangeBoundsU64.All), _state.IndexFormat);

        renderPass.DrawIndexed(0.._state.IndexCount, 0, 0.._state.InstanceCount);
    }

    private unsafe static Slice<byte> ShaderSource => Slice.FromFixedSpanUnsafe("""
        // Vertex shader
        struct Camera {
            view_proj: mat4x4<f32>,
        }
        @group(1) @binding(0)
        var<uniform> camera: Camera;

        struct VertexInput {
            @location(0) position: vec3<f32>,
            @location(1) tex_coords: vec2<f32>,
        }
        struct InstanceInput {
            @location(5) model_matrix_0: vec4<f32>,
            @location(6) model_matrix_1: vec4<f32>,
            @location(7) model_matrix_2: vec4<f32>,
            @location(8) model_matrix_3: vec4<f32>,
        }

        struct VertexOutput {
            @builtin(position) clip_position: vec4<f32>,
            @location(0) tex_coords: vec2<f32>,
        }

        @vertex
        fn vs_main(
            model: VertexInput,
            instance: InstanceInput,
        ) -> VertexOutput {
            let model_matrix = mat4x4<f32>(
                instance.model_matrix_0,
                instance.model_matrix_1,
                instance.model_matrix_2,
                instance.model_matrix_3,
            );
            var out: VertexOutput;
            out.tex_coords = model.tex_coords;
            out.clip_position = camera.view_proj * model_matrix * vec4<f32>(model.position, 1.0);
            return out;
        }

        // Fragment shader

        @group(0) @binding(0)
        var t_diffuse: texture_2d<f32>;
        @group(0)@binding(1)
        var s_diffuse: sampler;

        @fragment
        fn fs_main(in: VertexOutput) -> @location(0) vec4<f32> {
            return textureSample(t_diffuse, s_diffuse, in.tex_coords);
        }
            
        """u8);
}

internal static class HostScreenInitializer
{
    public unsafe static TextureHandle CreateTexture(HostScreenHandle screen, string filepath)
    {
        var pixelBytes = SamplePrimitives.LoadImagePixels(filepath, out var width, out var height);
        var size = new wgpu_Extent3d
        {
            width = width,
            height = height,
            depth_or_array_layers = 1,
        };
        var desc = new TextureDescriptor
        {
            dimension = TextureDimension.D2,
            format = TextureFormat.Rgba8UnormSrgb,
            mip_level_count = 1,
            sample_count = 1,
            size = size,
            usage = wgpu_TextureUsages.TEXTURE_BINDING | wgpu_TextureUsages.COPY_DST,
        };
        var texture = EngineCore.CreateTexture(screen, &desc);
        var writeTex = new ImageCopyTexture
        {
            texture = texture,
            mip_level = 0,
            aspect = TextureAspect.All,
            origin_x = 0,
            origin_y = 0,
            origin_z = 0,
        };
        var dataLayout = new wgpu_ImageDataLayout
        {
            offset = 0,
            bytes_per_row = 4 * width,
            rows_per_image = height,
        };
        fixed(byte* p = pixelBytes) {
            var data = new Slice<byte>(p, pixelBytes.Length);
            EngineCore.WriteTexture(screen, &writeTex, data, &dataLayout, &size);
        }
        return texture;
    }

    public unsafe static (TextureViewHandle TextureView, SamplerHandle Sampler) CreateTextureViewSampler(HostScreenHandle screen, TextureHandle texture)
    {
        var texViewDesc = TextureViewDescriptor.Default;
        var textureView = EngineCore.CreateTextureView(screen, texture, &texViewDesc);

        var samplerDesc = new SamplerDescriptor
        {
            address_mode_u = wgpu_AddressMode.ClampToEdge,
            address_mode_v = wgpu_AddressMode.ClampToEdge,
            address_mode_w = wgpu_AddressMode.ClampToEdge,
            mag_filter = wgpu_FilterMode.Linear,
            min_filter = wgpu_FilterMode.Nearest,
            mipmap_filter = wgpu_FilterMode.Nearest,
            anisotropy_clamp = 0,
            lod_max_clamp = 0,
            lod_min_clamp = 0,
            border_color = Opt.None<SamplerBorderColor>(),
            compare = Opt.None<wgpu_CompareFunction>(),
        };
        var sampler = EngineCore.CreateSampler(screen, &samplerDesc);

        return (TextureView: textureView, Sampler: sampler);
    }

    public unsafe static BindGroupLayoutHandle CreateTextureBindGroupLayout(HostScreenHandle screen)
    {
        var desc = new BindGroupLayoutDescriptor
        {
            entries = Slice.FromFixedSpanUnsafe(stackalloc BindGroupLayoutEntry[2]
            {
                new()
                {
                    binding = 0,
                    visibility = wgpu_ShaderStages.FRAGMENT,
                    ty = BindingType.Texture(UnsafeEx.StackPointer(new TextureBindingData
                    {
                        multisampled = false,
                        view_dimension = TextureViewDimension.D2,
                        sample_type = TextureSampleType.FloatFilterable,
                    })),
                    count = 0,
                },
                new()
                {
                    binding = 1,
                    visibility = wgpu_ShaderStages.FRAGMENT,
                    ty = BindingType.Sampler(UnsafeEx.StackPointer(SamplerBindingType.Filtering)),
                    count = 0,
                },
            }),
        };
        return EngineCore.CreateBindGroupLayout(screen, &desc);
    }

    public unsafe static BindGroupHandle CreateTextureBindGroup(
        HostScreenHandle screen,
        BindGroupLayoutHandle bindGroupLayout,
        TextureViewHandle textureView,
        SamplerHandle sampler)
    {
        var desc = new BindGroupDescriptor
        {
            layout = bindGroupLayout,
            entries = Slice.FromFixedSpanUnsafe((stackalloc BindGroupEntry[2]
            {
                new()
                {
                    binding = 0,
                    resource = BindingResource.TextureView(textureView),
                },
                new()
                {
                    binding = 1,
                    resource = BindingResource.Sampler(sampler),
                },
            })),
        };
        return EngineCore.CreateBindGroup(screen, &desc);
    }

    public unsafe static BufferHandle CreateUniformBuffer<T>(HostScreenHandle screen, Span<T> data) where T : unmanaged
        => CreateUniformBuffer(screen, (ReadOnlySpan<T>)data);

    public unsafe static BufferHandle CreateUniformBuffer<T>(HostScreenHandle screen, ReadOnlySpan<T> data) where T : unmanaged
    {
        var bytes = data.AsBytes();
        fixed(byte* p = bytes) {
            var contents = new Slice<byte>(p, (nuint)bytes.Length);
            var usage = wgpu_BufferUsages.UNIFORM | wgpu_BufferUsages.COPY_DST;
            return EngineCore.CreateBufferInit(screen, contents, usage);
        }
    }

    public unsafe static BindGroupLayoutHandle CreateUniformBindGroupLayout(HostScreenHandle screen)
    {
        var desc = new BindGroupLayoutDescriptor
        {
            entries = Slice.FromFixedSpanUnsafe(stackalloc BindGroupLayoutEntry[1]
                {
                new()
                {
                    binding = 0,
                    visibility = wgpu_ShaderStages.VERTEX_FRAGMENT,
                    ty = BindingType.Buffer(UnsafeEx.StackPointer(new BufferBindingData
                    {
                        ty = BufferBindingType.Uniform,
                        has_dynamic_offset = false,
                        min_binding_size = 0,
                    })),
                    count = 0,
                },
            }),
        };
        return EngineCore.CreateBindGroupLayout(screen, &desc);
    }

    //public unsafe static BindGroupHandle CreateUniformBindGroup(
    //    HostScreenHandle screen,
    //    BindGroupLayoutHandle layout,
    //    BufferHandle buffer)
    //{
    //    var desc = new BindGroupDescriptor
    //    {
    //        layout = layout,
    //        entries = Slice.FromFixedSpanUnsafe((stackalloc BindGroupEntry[1]
    //        {
    //            new()
    //            {
    //                binding = 0,
    //                resource = BindingResource.Buffer(UnsafeEx.StackPointer(
    //                    buffer.AsEntriesBinding()
    //                )),
    //            },
    //        })),
    //    };
    //    return EngineCore.CreateBindGroup(screen, &desc);
    //}

    public unsafe static (
        BufferHandle VertexBuffer,
        uint VertexCount,
        BufferHandle IndexBuffer,
        uint IndexCount,
        wgpu_IndexFormat IndexFormat
    ) CreateVertexIndexBuffer<TVertex>(
            HostScreenHandle screen,
            ReadOnlySpan<TVertex> vertices,
            ReadOnlySpan<uint> indices) where TVertex : unmanaged
    {
        BufferHandle vertexBuffer;
        uint vertexCount;
        fixed(TVertex* v = vertices) {
            nuint bytelen = (nuint)sizeof(TVertex) * (nuint)vertices.Length;
            var contents = new Slice<byte>(v, bytelen);
            vertexBuffer = EngineCore.CreateBufferInit(screen, contents, wgpu_BufferUsages.VERTEX);
            vertexCount = (uint)vertices.Length;
        }

        BufferHandle indexBuffer;
        uint indexCount;
        const wgpu_IndexFormat indexFormat = wgpu_IndexFormat.Uint32;
        fixed(uint* i = indices) {
            nuint bytelen = (nuint)sizeof(uint) * (nuint)indices.Length;
            var contents = new Slice<byte>(i, bytelen);
            indexBuffer = EngineCore.CreateBufferInit(screen, contents, wgpu_BufferUsages.INDEX);
            indexCount = (uint)indices.Length;
        }

        return (
            VertexBuffer: vertexBuffer,
            VertexCount: vertexCount,
            IndexBuffer: indexBuffer,
            IndexCount: indexCount,
            IndexFormat: indexFormat
        );
    }
}

internal class Camera
{
    public required Vector3 eye;
    public required Vector3 target;
    public required Vector3 up;
    public required float aspect;
    public required float fovy;
    public required float znear;
    public required float zfar;

    public Matrix4 BuildViewProjMatrix()
    {
        var view = Matrix4.LookAt(eye, target, up);
        Matrix4.PerspectiveProjection(
            fovy: fovy,
            aspect: aspect,
            depthNear: znear,
            depthFar: zfar,
            out var proj);
        return proj * view;
    }
}

internal struct CameraUniform
{
    public Matrix4 ViewProj;

    public static readonly Matrix4 OPENGL_TO_WGPU_MATRIX = new Matrix4(
        1.0f, 0.0f, 0.0f, 0.0f,
        0.0f, 1.0f, 0.0f, 0.0f,
        0.0f, 0.0f, 0.5f, 0.5f,
        0.0f, 0.0f, 0.0f, 1.0f);

    public static CameraUniform Default => new() { ViewProj = Matrix4.Identity };

    public void UpdateViewProj(Camera camera)
    {
        ViewProj = OPENGL_TO_WGPU_MATRIX * camera.BuildViewProjMatrix();
    }
}

internal struct InstanceRaw
{
    public required Matrix4 Model;
}

internal struct State
{
    public required PipelineLayoutHandle PipelineLayout;
    public required ShaderModuleHandle Shader;
    public required RenderPipelineHandle RenderPipeline;

    public required BufferHandle VertexBuffer;
    //public required uint VertexCount;
    public required BufferHandle IndexBuffer;
    public required int IndexCount;
    public required wgpu_IndexFormat IndexFormat;

    public required TextureHandle DiffuseTexture;
    public required TextureViewHandle TextureView;
    public required SamplerHandle Sampler;
    public required BindGroupLayoutHandle TextureBindGroupLayout;
    public required BindGroupHandle DiffuseBindGroup;

    public required BufferHandle CameraBuffer;
    public required BindGroupLayoutHandle CameraBindGroupLayout;
    public required BindGroupHandle CameraBindGroup;

    public required BufferHandle InstanceBuffer;
    public required int InstanceCount;
}


internal unsafe static class UnsafeEx
{
    public static T* StackPointer<T>(in T x) where T : unmanaged
    {
        return (T*)Unsafe.AsPointer(ref Unsafe.AsRef(in x));
    }
}

internal static class SpanExtensions
{
    public static ReadOnlySpan<T> AsReadOnly<T>(this Span<T> span) where T : unmanaged
    {
        return (ReadOnlySpan<T>)span;
    }

    public static ReadOnlySpan<byte> AsBytes<T>(this ReadOnlySpan<T> span) where T : unmanaged
    {
        return MemoryMarshal.AsBytes(span);
    }
}
