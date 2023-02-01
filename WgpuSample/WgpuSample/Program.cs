#nullable enable
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Elffy.Bind;
using System.Diagnostics.CodeAnalysis;

namespace Elffy;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Environment.SetEnvironmentVariable("RUST_BACKTRACE", "1", EnvironmentVariableTarget.Process);
        var engineConfig = new EngineCoreConfig
        {
            OnStart = OnStart,
            OnRedrawRequested = OnRedrawRequested,
            OnCleared = (id) =>
            {
                if(TryGetState(id, out var state) == false) {
                    return;
                }
                var screen = state.Screen.AsRef();
                screen.ScreenRequestRedraw();
            },
            OnResized = OnResized,
        };
        var screenConfig = new HostScreenConfig
        {
            Backend = Wgpu.Backends.DX12,
            Width = 1280,
            Height = 720,
            Style = WindowStyle.Default,
        };
        EngineCore.EngineStart(engineConfig, screenConfig);
    }

    private static readonly List<State> _stateList = new List<State>();
    private static bool TryGetState(CE.HostScreenId id, [MaybeNullWhen(false)] out State state)
    {
        var list = (ReadOnlySpan<State>)CollectionsMarshal.AsSpan(_stateList);
        foreach(var s in list) {
            if(s.Id == id) {
                state = s;
                return true;
            }
        }
        state = null;
        return false;
    }

    const int NUM_INSTANCES_PER_ROW = 10;
    private static readonly Vector3 INSTANCE_DISPLACEMENT = new(
        NUM_INSTANCES_PER_ROW * 0.5f, 0, NUM_INSTANCES_PER_ROW * 0.5f
        );

    private unsafe static void OnRedrawRequested(CE.HostScreenId id)
    {
        if(TryGetState(id, out var state) == false) {
            return;
        }
        var screenRef = state.Screen.AsRef();

        {
            if(screenRef.ScreenBeginCommand(out var commandEncoder, out var surfaceTexture, out var surfaceTextureView) == false) {
                return;
            }
            {
                RenderPassDescriptor desc;
                {
                    const int ColorAttachmentCount = 1;
                    var colorAttachments = stackalloc Opt_RenderPassColorAttachment[ColorAttachmentCount]
                    {
                        new()
                        {
                            exists = true,
                            value = new RenderPassColorAttachment
                            {
                                view = surfaceTextureView,
                                clear = new Wgpu.Color(0, 0, 0, 0),
                            }
                        },
                    };
                    desc = new RenderPassDescriptor
                    {
                        color_attachments_clear = new() { data = colorAttachments, len = ColorAttachmentCount },
                        depth_stencil_attachment_clear = new()
                        {
                            exists = true,
                            value = new RenderPassDepthStencilAttachment
                            {
                                view = state.GetDepth().View,
                                depth_clear = Opt<float>.Some(1f),
                                stencil_clear = Opt<uint>.None,
                            }
                        }
                    };
                }

                var renderPass = commandEncoder.AsMut().CreateRenderPass(desc);
                try {
                    var r = renderPass.AsMut();
                    r.SetPipeline(state.RenderPipeline);
                    r.SetBindGroup(0, state.DiffuseBindGroup);
                    r.SetBindGroup(1, state.CameraBindGroup);
                    r.SetVertexBuffer(0, state.VertexBuffer.AsRef().AsSlice());
                    r.SetVertexBuffer(1, state.InstanceBuffer.AsRef().AsSlice());
                    r.SetIndexBuffer(state.IndexBuffer.AsRef().AsSlice(), state.IndexFormat);
                    r.DrawIndexed(0..state.IndexCount, 0, 0..state.InstanceCount);
                }
                finally {
                    renderPass.DestroyRenderPass();
                }
            }
            screenRef.ScreenFinishCommand(commandEncoder, surfaceTexture, surfaceTextureView);
        }
    }

    private static unsafe void OnStart(Box<CE.HostScreen> screen, CE.HostScreenInfo info, CE.HostScreenId id)
    {
        var screenRef = screen.AsRef();
        screenRef.ScreenSetTitle("sample"u8);

        var surfaceFormat = info.surface_format.Unwrap();
        System.Diagnostics.Debug.WriteLine(info.backend);


        // Texture, TextureView, Sampler
        var textureData = HostScreenInitializer.CreateTexture(screenRef, "happy-tree.png");

        //// TextureView, Sampler
        //var (textureView, sampler) = HostScreenInitializer.CreateTextureViewSampler(screen, diffuseTexture);

        // BindGroupLayout
        var textureBindGroupLayout = HostScreenInitializer.CreateTextureBindGroupLayout(screenRef);

        // BindGroup
        var diffuseBindGroup = HostScreenInitializer.CreateTextureBindGroup(screenRef, textureBindGroupLayout, textureData.View, textureData.Sampler);

        var screenSize = screenRef.ScreenGetInnerSize();
        var camera = new Camera
        {
            eye = new(0.0f, 5.0f, -10.0f),
            target = new(0, 0, 0),
            up = new(0, 1, 0),
            aspect = (float)screenSize.Width / screenSize.Height,
            fovy = 45f / 180f * float.Pi,
            znear = 0.1f,
            zfar = 100f,
        };
        var cameraUniform = CameraUniform.Default;
        cameraUniform.UpdateViewProj(camera);

        var cameraBuffer = screenRef.CreateBufferInit(
            new Slice<byte>((byte*)&cameraUniform, sizeof(CameraUniform)),
            Wgpu.BufferUsages.UNIFORM | Wgpu.BufferUsages.COPY_DST);

        //var instances = Enumerable
        //    .Range(0, NUM_INSTANCES_PER_ROW)
        //    .SelectMany(z => Enumerable.Range(0, NUM_INSTANCES_PER_ROW).Select(x =>
        //    {
        //        var position = new Vector3(x, 0, z) - INSTANCE_DISPLACEMENT;
        //        var rotation = position.IsZero ?
        //            Quaternion.FromAxisAngle(Vector3.UnitZ, 0) :
        //            Quaternion.FromAxisAngle(position.Normalized(), 45f / 180f * float.Pi);
        //        var scale = Vector3.One;
        //        var model = position.ToTranslationMatrix4() * rotation.ToMatrix4() * scale.ToScaleMatrix4();
        //        return new InstanceRaw
        //        {
        //            Model = model,
        //        };
        //    }))
        //    .ToArray();
        var instances = new InstanceData[2]
        {
            new InstanceData(new Vector3(0.5f, 0, 0)),
            new InstanceData(new Vector3(-0.5f, 0, 0)),
        };

        // Buffer (instance)
        Box<Wgpu.Buffer> instanceBuffer;
        int instanceCount = instances.Length;
        fixed(InstanceData* instanceData = instances) {
            instanceBuffer = screenRef.CreateBufferInit(
                new Slice<byte>((byte*)instanceData, sizeof(InstanceData) * instances.Length),
                Wgpu.BufferUsages.VERTEX | Wgpu.BufferUsages.COPY_DST);
        }

        var cameraBindGroupLayout = screenRef.CreateBindGroupLayout(new()
        {
            entries = Slice.FromFixedSpanUnsafe(stackalloc BindGroupLayoutEntry[1]
            {
                new()
                {
                    binding = 0,
                    visibility = Wgpu.ShaderStages.VERTEX,
                    ty = BindingType.Buffer(UnsafeEx.StackPointer(new BufferBindingData
                    {
                        ty = BufferBindingType.Uniform,
                        has_dynamic_offset = false,
                        min_binding_size = 0,
                    })),
                    count = 0,
                },
            }),
        });

        Box<Wgpu.BindGroup> cameraBindGroup;
        {
            var bufferBinding = cameraBuffer.AsRef().AsEntireBufferBinding();
            const int EntryCount = 1;
            var entries = stackalloc BindGroupEntry[EntryCount]
            {
                new() { binding = 0, resource = BindingResource.Buffer(&bufferBinding), }
            };
            cameraBindGroup = screenRef.CreateBindGroup(new BindGroupDescriptor
            {
                layout = cameraBindGroupLayout,
                entries = new() { data = entries, len = EntryCount },
            });
        }

        var shader = screenRef.CreateShaderModule(ShaderSource);

        //// BindGroupLayout (uniform)
        //var uniformBindGroupLayout = HostScreenInitializer.CreateUniformBindGroupLayout(screen);

        // PipelineLayout
        Box<Wgpu.PipelineLayout> pipelineLayout;
        {
            const int BindGroupLayoutCount = 2;
            var bindGroupLayouts = stackalloc Ref<Wgpu.BindGroupLayout>[BindGroupLayoutCount] { textureBindGroupLayout, cameraBindGroupLayout };
            pipelineLayout = screenRef.CreatePipelineLayout(new PipelineLayoutDescriptor
            {
                bind_group_layouts = new()
                {
                    data = bindGroupLayouts,
                    len = BindGroupLayoutCount,
                },
            });
        }

        //// Buffer (uniform)
        //var uniformBuffer = HostScreenInitializer.CreateUniformBuffer(screen, stackalloc Vector4[] { new Vector4(1, 0, 0, 1) });

        //// BindGroup (uniform)
        //var uniformBindGroup = HostScreenInitializer.CreateUniformBindGroup(screen, uniformBindGroupLayout, uniformBuffer);

        var depthTextureData = CreateDepthTexture(screenRef, screenSize.Width, screenSize.Height);

        // RenderPipeline
        Box<Wgpu.RenderPipeline> renderPipeline;
        {
            var vertexBufferLayout = new VertexBufferLayout
            {
                array_stride = (ulong)sizeof(Vertex),
                step_mode = Wgpu.VertexStepMode.Vertex,
                attributes = Slice.FromFixedSpanUnsafe(stackalloc Wgpu.VertexAttribute[2]
                {
                    new() { offset = 0, shader_location = 0, format = Wgpu.VertexFormat.Float32x3 },
                    new() { offset = 12, shader_location = 1, format = Wgpu.VertexFormat.Float32x2 },
                }),
            };
            //var instanceBufferLayout = new VertexBufferLayout
            //{
            //    array_stride = (ulong)sizeof(InstanceRaw),
            //    step_mode = Wgpu.VertexStepMode.Instance,
            //    attributes = Slice.FromFixedSpanUnsafe(stackalloc Wgpu.VertexAttribute[]
            //    {
            //        new() { offset = 4 * 0, shader_location = 5, format = Wgpu.VertexFormat.Float32x4 },
            //        new() { offset = 4 * 4, shader_location = 6, format = Wgpu.VertexFormat.Float32x4 },
            //        new() { offset = 4 * 8, shader_location = 7, format = Wgpu.VertexFormat.Float32x4 },
            //        new() { offset = 4 * 12, shader_location = 8, format = Wgpu.VertexFormat.Float32x4 },
            //    }),
            //};
            var instanceBufferLayout = new VertexBufferLayout
            {
                array_stride = (ulong)sizeof(InstanceData),
                step_mode = Wgpu.VertexStepMode.Instance,
                attributes = Slice.FromFixedSpanUnsafe(stackalloc Wgpu.VertexAttribute[]
                {
                    new() { offset = 0, shader_location = 5, format = Wgpu.VertexFormat.Float32x3 },
                }),
            };


            renderPipeline = screenRef.CreateRenderPipeline(new RenderPipelineDescriptor
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
                fragment = new()
                {
                    exists = true,
                    value = new FragmentState()
                    {
                        module = shader,
                        entry_point = Slice.FromFixedSpanUnsafe("fs_main"u8),
                        targets = Slice.FromFixedSpanUnsafe(stackalloc Opt<ColorTargetState>[]
                        {
                            Opt<ColorTargetState>.Some(new()
                            {
                                format = surfaceFormat,
                                blend = Opt<Wgpu.BlendState>.Some(Wgpu.BlendState.REPLACE),
                                write_mask = Wgpu.ColorWrites.ALL,
                            })
                        }),
                    }
                },
                primitive = new()
                {
                    topology = Wgpu.PrimitiveTopology.TriangleList,
                    strip_index_format = Opt<Wgpu.IndexFormat>.None,
                    front_face = Wgpu.FrontFace.Ccw,
                    cull_mode = Opt<Wgpu.Face>.Some(Wgpu.Face.Back),
                    polygon_mode = Wgpu.PolygonMode.Fill,
                },
                depth_stencil = Opt<DepthStencilState>.Some(new()
                {
                    format = depthTextureData.Format,
                    depth_write_enabled = true,
                    depth_compare = Wgpu.CompareFunction.Less,
                    stencil = Wgpu.StencilState.Default,
                    bias = Wgpu.DepthBiasState.Default,
                }),
                multisample = Wgpu.MultisampleState.Default,
                multiview = NonZeroU32OrNone.None,
            });
        }

        // Buffer (vertex, index)
        Box<Wgpu.Buffer> vertexBuffer;
        Box<Wgpu.Buffer> indexBuffer;
        int indexCount;
        Wgpu.IndexFormat indexFormat;
        {
            var (vertices, indices) = SamplePrimitives.SampleData();
            indexCount = indices.Length;
            fixed(Vertex* vData = vertices) {
                vertexBuffer = screenRef.CreateBufferInit(
                    new Slice<byte>((byte*)vData, sizeof(Vertex) * vertices.Length),
                    Wgpu.BufferUsages.VERTEX);
            }
            fixed(ushort* iData = indices) {
                indexBuffer = screenRef.CreateBufferInit(
                    new Slice<byte>((byte*)iData, sizeof(ushort) * indices.Length),
                    Wgpu.BufferUsages.INDEX);
            }
            indexFormat = Wgpu.IndexFormat.Uint16;
        }



        var state = new State
        {
            Id = id,
            Screen = screen,
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
            DiffuseTextureData = textureData,
            TextureBindGroupLayout = textureBindGroupLayout,
            DiffuseBindGroup = diffuseBindGroup,

            CameraBuffer = cameraBuffer,
            CameraBindGroupLayout = cameraBindGroupLayout,
            CameraBindGroup = cameraBindGroup,
            Depth = depthTextureData,
            //UniformBuffer = uniformBuffer,
            //UniformBindGroupLayout = uniformBindGroupLayout,
            //UniformBindGroup = uniformBindGroup,
        };
        _stateList.Add(state);
    }

    private static void OnResized(CE.HostScreenId id, uint width, uint height)
    {
        if(TryGetState(id, out var state) == false) {
            return;
        }
        var screen = state.Screen.AsRef();
        screen.ScreenResizeSurface(width, height);



        // Ref<CE.HostScreen> screen

        //var depth = _state.Depth;
        ref readonly var depth = ref state.GetDepth();
        //if(width == depth.Width && height == depth.Height) { return; }

        Debug.WriteLine((width, height));
        var newDepth = CreateDepthTexture(screen, width, height);
        state.SetDepth(newDepth);
    }

    ////[StructLayout(LayoutKind.Sequential, Size = 4)]
    //private struct InstanceData
    //{
    //    public Vector3 Value;
    //}

    private static DepthTextureData CreateDepthTexture(Ref<CE.HostScreen> screen, uint width, uint height)
    {
        const TextureFormat DepthTextureFormat = TextureFormat.Depth32Float;
        var texture = screen.CreateTexture(new TextureDescriptor
        {
            size = new Wgpu.Extent3d
            {
                width = width,
                height = height,
                depth_or_array_layers = 1,
            },
            mip_level_count = 1,
            sample_count = 1,
            dimension = TextureDimension.D2,
            format = DepthTextureFormat,
            usage = Wgpu.TextureUsages.RENDER_ATTACHMENT | Wgpu.TextureUsages.TEXTURE_BINDING,
        });
        var view = texture.AsRef().CreateTextureView();
        var sampler = screen.CreateSampler(new SamplerDescriptor
        {
            address_mode_u = Wgpu.AddressMode.ClampToEdge,
            address_mode_v = Wgpu.AddressMode.ClampToEdge,
            address_mode_w = Wgpu.AddressMode.ClampToEdge,
            mag_filter = Wgpu.FilterMode.Linear,
            min_filter = Wgpu.FilterMode.Linear,
            mipmap_filter = Wgpu.FilterMode.Nearest,
            compare = Opt<Wgpu.CompareFunction>.Some(Wgpu.CompareFunction.LessEqual),
            lod_min_clamp = 0f,
            lod_max_clamp = 100f,
        });
        return new DepthTextureData(width, height, texture, view, sampler, DepthTextureFormat);
    }

    private unsafe static ReadOnlySpan<byte> ShaderSource => """
        // Vertex shader
        struct Camera {
            view_proj: mat4x4<f32>,
        }
        @group(1) @binding(0)
        var<uniform> camera: Camera;

        struct Vertex {
            @location(0) position: vec3<f32>,
            @location(1) tex_coords: vec2<f32>,
        }
        struct InstanceData {
            @location(5) offset: vec3<f32>,
        }

        struct VertexOutput {
            @builtin(position) clip_position: vec4<f32>,
            @location(0) tex_coords: vec2<f32>,
        }

        @vertex
        fn vs_main(
            v: Vertex,
            instance: InstanceData,
        ) -> VertexOutput {
            var out: VertexOutput;
            out.clip_position = vec4<f32>(v.position + instance.offset, 1.0);
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
            
        """u8;
}

internal static class HostScreenInitializer
{
    public unsafe static TextureData CreateTexture(Ref<CE.HostScreen> screen, string filepath)
    {
        var (pixelBytes, width, height) = SamplePrimitives.LoadImagePixels(filepath);
        var size = new Wgpu.Extent3d
        {
            width = width,
            height = height,
            depth_or_array_layers = 1,
        }; ;
        var texture = screen.CreateTexture(new()
        {
            dimension = TextureDimension.D2,
            format = TextureFormat.Rgba8UnormSrgb,
            mip_level_count = 1,
            sample_count = 1,
            size = size,
            usage = Wgpu.TextureUsages.TEXTURE_BINDING | Wgpu.TextureUsages.COPY_DST,
        });
        fixed(byte* p = pixelBytes) {
            screen.WriteTexture(
                new ImageCopyTexture
                {
                    texture = texture,
                    mip_level = 0,
                    aspect = TextureAspect.All,
                    origin_x = 0,
                    origin_y = 0,
                    origin_z = 0,
                },
                new Slice<byte>(p, pixelBytes.Length),
                new Wgpu.ImageDataLayout
                {
                    offset = 0,
                    bytes_per_row = 4 * width,
                    rows_per_image = height,
                },
                size);
        }

        var textureView = texture.AsRef().CreateTextureView(TextureViewDescriptor.Default);
        var sampler = screen.CreateSampler(new SamplerDescriptor
        {
            address_mode_u = Wgpu.AddressMode.ClampToEdge,
            address_mode_v = Wgpu.AddressMode.ClampToEdge,
            address_mode_w = Wgpu.AddressMode.ClampToEdge,
            mag_filter = Wgpu.FilterMode.Linear,
            min_filter = Wgpu.FilterMode.Nearest,
            mipmap_filter = Wgpu.FilterMode.Nearest,
            anisotropy_clamp = 0,
            lod_max_clamp = 0,
            lod_min_clamp = 0,
            border_color = Opt<SamplerBorderColor>.None,
            compare = Opt<Wgpu.CompareFunction>.None,
        });
        return new TextureData
        {
            Texture = texture,
            Sampler = sampler,
            View = textureView,
        };
    }

    //public unsafe static (Box<Wgpu.TextureView> TextureView, Box<Wgpu.Sampler> Sampler) CreateTextureViewSampler(
    //    Ref<CE.HostScreen> screen,
    //    Ref<Wgpu.Texture> texture)
    //{
    //    var textureView = texture.CreateTextureView(TextureViewDescriptor.Default);
    //    var sampler = screen.CreateSampler(new SamplerDescriptor
    //    {
    //        address_mode_u = Wgpu.AddressMode.ClampToEdge,
    //        address_mode_v = Wgpu.AddressMode.ClampToEdge,
    //        address_mode_w = Wgpu.AddressMode.ClampToEdge,
    //        mag_filter = Wgpu.FilterMode.Linear,
    //        min_filter = Wgpu.FilterMode.Nearest,
    //        mipmap_filter = Wgpu.FilterMode.Nearest,
    //        anisotropy_clamp = 0,
    //        lod_max_clamp = 0,
    //        lod_min_clamp = 0,
    //        border_color = Opt.None<SamplerBorderColor>(),
    //        compare = Opt.None<Wgpu.CompareFunction>(),
    //    });

    //    return (TextureView: textureView, Sampler: sampler);
    //}

    public unsafe static Box<Wgpu.BindGroupLayout> CreateTextureBindGroupLayout(Ref<CE.HostScreen> screen)
    {
        return screen.CreateBindGroupLayout(new()
        {
            entries = Slice.FromFixedSpanUnsafe(stackalloc BindGroupLayoutEntry[2]
            {
                new()
                {
                    binding = 0,
                    visibility = Wgpu.ShaderStages.FRAGMENT,
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
                    visibility = Wgpu.ShaderStages.FRAGMENT,
                    ty = BindingType.Sampler(UnsafeEx.StackPointer(SamplerBindingType.Filtering)),
                    count = 0,
                },
            }),
        });
    }

    public unsafe static Box<Wgpu.BindGroup> CreateTextureBindGroup(
        Ref<CE.HostScreen> screen,
        Ref<Wgpu.BindGroupLayout> bindGroupLayout,
        Ref<Wgpu.TextureView> textureView,
        Ref<Wgpu.Sampler> sampler)
    {
        const int EntryCount = 2;
        var entries = stackalloc BindGroupEntry[EntryCount]
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
        };

        return screen.CreateBindGroup(new BindGroupDescriptor
        {
            layout = bindGroupLayout,
            entries = new() { data = entries, len = EntryCount }
        });
    }

    public unsafe static Box<Wgpu.Buffer> CreateUniformBuffer<T>(Ref<CE.HostScreen> screen, Span<T> data) where T : unmanaged
        => CreateUniformBuffer(screen, (ReadOnlySpan<T>)data);

    public unsafe static Box<Wgpu.Buffer> CreateUniformBuffer<T>(Ref<CE.HostScreen> screen, ReadOnlySpan<T> data) where T : unmanaged
    {
        fixed(T* p = data) {
            var contents = new Slice<byte>((byte*)p, (nuint)data.Length * (nuint)sizeof(T));
            var usage = Wgpu.BufferUsages.UNIFORM | Wgpu.BufferUsages.COPY_DST;
            return screen.CreateBufferInit(contents, usage);
        }
    }

    public unsafe static Box<Wgpu.BindGroupLayout> CreateUniformBindGroupLayout(Ref<CE.HostScreen> screen)
    {
        return screen.CreateBindGroupLayout(new()
        {
            entries = Slice.FromFixedSpanUnsafe(stackalloc BindGroupLayoutEntry[1]
                {
                new()
                {
                    binding = 0,
                    visibility = Wgpu.ShaderStages.VERTEX_FRAGMENT,
                    ty = BindingType.Buffer(UnsafeEx.StackPointer(new BufferBindingData
                    {
                        ty = BufferBindingType.Uniform,
                        has_dynamic_offset = false,
                        min_binding_size = 0,
                    })),
                    count = 0,
                },
            }),
        });
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
        Box<Wgpu.Buffer> VertexBuffer,
        uint VertexCount,
        Box<Wgpu.Buffer> IndexBuffer,
        uint IndexCount,
        Wgpu.IndexFormat IndexFormat
    ) CreateVertexIndexBuffer<TVertex>(
            Ref<CE.HostScreen> screen,
            ReadOnlySpan<TVertex> vertices,
            ReadOnlySpan<uint> indices) where TVertex : unmanaged
    {
        Box<Wgpu.Buffer> vertexBuffer;
        uint vertexCount;
        fixed(TVertex* v = vertices) {
            nuint bytelen = (nuint)sizeof(TVertex) * (nuint)vertices.Length;
            var contents = new Slice<byte>((byte*)v, bytelen);
            vertexBuffer = screen.CreateBufferInit(contents, Wgpu.BufferUsages.VERTEX);
            vertexCount = (uint)vertices.Length;
        }

        Box<Wgpu.Buffer> indexBuffer;
        uint indexCount;
        const Wgpu.IndexFormat indexFormat = Wgpu.IndexFormat.Uint32;
        fixed(uint* i = indices) {
            nuint bytelen = (nuint)sizeof(uint) * (nuint)indices.Length;
            var contents = new Slice<byte>((byte*)i, bytelen);
            indexBuffer = screen.CreateBufferInit(contents, Wgpu.BufferUsages.INDEX);
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

internal record struct InstanceData(Vector3 Offset);

internal sealed class State
{
    public required CE.HostScreenId Id { get; init; }
    public required Box<CE.HostScreen> Screen { get; init; }

    public required Box<Wgpu.PipelineLayout> PipelineLayout { get; init; }
    public required Box<Wgpu.ShaderModule> Shader { get; init; }
    public required Box<Wgpu.RenderPipeline> RenderPipeline { get; init; }

    public required Box<Wgpu.Buffer> VertexBuffer { get; init; }
    //public required uint VertexCount;
    public required Box<Wgpu.Buffer> IndexBuffer { get; init; }
    public required int IndexCount;
    public required Wgpu.IndexFormat IndexFormat { get; init; }

    public required TextureData DiffuseTextureData { get; init; }


    public required Box<Wgpu.BindGroupLayout> TextureBindGroupLayout { get; init; }
    public required Box<Wgpu.BindGroup> DiffuseBindGroup { get; init; }

    public required Box<Wgpu.Buffer> CameraBuffer { get; init; }
    public required Box<Wgpu.BindGroupLayout> CameraBindGroupLayout { get; init; }
    public required Box<Wgpu.BindGroup> CameraBindGroup { get; init; }

    public required Box<Wgpu.Buffer> InstanceBuffer { get; init; }
    public required int InstanceCount { get; init; }

    private DepthTextureData _depth;
    public required DepthTextureData Depth { init => _depth = value; }
    public ref readonly DepthTextureData GetDepth() => ref _depth;
    public void SetDepth(in DepthTextureData depth)
    {
        EngineCore.DestroySampler(_depth.Sampler);
        EngineCore.DestroyTextureView(_depth.View);
        EngineCore.DestroyTexture(_depth.Texture);
        _depth = depth;
    }
}

internal record struct TextureData(
    Box<Wgpu.Texture> Texture,
    Box<Wgpu.TextureView> View,
    Box<Wgpu.Sampler> Sampler);

internal record struct DepthTextureData(
    uint Width,
    uint Height,
    Box<Wgpu.Texture> Texture,
    Box<Wgpu.TextureView> View,
    Box<Wgpu.Sampler> Sampler,
    TextureFormat Format);


internal unsafe static class UnsafeEx
{
    public static T* StackPointer<T>(in T x) where T : unmanaged
    {
        return (T*)Unsafe.AsPointer(ref Unsafe.AsRef(in x));
    }
}
