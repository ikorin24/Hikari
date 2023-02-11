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
        Environment.SetEnvironmentVariable("RUST_BACKTRACE", "1");
        //var engineConfig = new EngineCoreConfig
        //{
        //    OnStart = OnStart,
        //    OnRedrawRequested = OnRedrawRequested,
        //    OnCleared = (id) =>
        //    {
        //        if(TryGetState(id, out var state) == false) {
        //            return;
        //        }
        //        var screen = state.Screen.AsRef();
        //        screen.ScreenRequestRedraw();
        //    },
        //    OnResized = OnResized,
        //};
        //var screenConfig = new HostScreenConfig
        //{
        //    Backend = GraphicsBackend.Dx12,
        //    Width = 1280,
        //    Height = 720,
        //    Style = WindowStyle.Default,
        //};
        //EngineCore.EngineStart(engineConfig, screenConfig);

        var screenConfig = new HostScreenConfig
        {
            Backend = GraphicsBackend.Dx12,
            Width = 1280,
            Height = 720,
            Style = WindowStyle.Default,
        };
        Engine.Start(screenConfig, OnInitialized);
    }

    private static State2? _state2;

    private unsafe static void OnRedrawRequested(Ref<CE.HostScreen> screenRef)
    {
        if(screenRef.ScreenBeginCommand(out var commandEncoder, out var surfaceTexture, out var surfaceTextureView) == false) {
            return;
        }
        var state = _state2;
        if(state == null) { return; }
        {
            CE.RenderPassDescriptor desc;
            {
                const int ColorAttachmentCount = 1;
                var colorAttachments = stackalloc Opt<CE.RenderPassColorAttachment>[ColorAttachmentCount]
                {
                    new(new CE.RenderPassColorAttachment
                    {
                        view = surfaceTextureView,
                        clear = new Wgpu.Color(0, 0, 0, 0),
                    }),
                };
                desc = new CE.RenderPassDescriptor
                {
                    color_attachments_clear = new() { data = colorAttachments, len = ColorAttachmentCount },
                    depth_stencil_attachment_clear = new(new CE.RenderPassDepthStencilAttachment
                    {
                        view = state.DepthTextureView.NativeRef,
                        depth_clear = Opt<float>.Some(1f),
                        stencil_clear = Opt<uint>.None,
                    }),
                };
            }

            var renderPass = commandEncoder.AsMut().CreateRenderPass(desc);
            try {
                var r = renderPass.AsMut();
                r.SetPipeline(state.RenderPipeline.NativeRef);
                r.SetBindGroup(0, state.BindGroup.NativeRef);
                r.SetVertexBuffer(0, state.VertexBuffer.NativeRef.AsSlice());
                r.SetVertexBuffer(1, state.InstanceBuffer.NativeRef.AsSlice());
                r.SetIndexBuffer(state.IndexBuffer.NativeRef.AsSlice(), state.IndexFormat.MapOrThrow());
                r.DrawIndexed(0..state.IndexCount, 0, 0..state.InstanceCount);
            }
            finally {
                renderPass.DestroyRenderPass();
            }
        }
        screenRef.ScreenFinishCommand(commandEncoder, surfaceTexture, surfaceTextureView);

    }

    private static void OnInitialized(IHostScreen screen)
    {
        screen.RedrawRequested += (screen) =>
        {
            OnRedrawRequested(screen.AsRefChecked());
        };
        screen.Resized += (screen, w, h) =>
        {
            var state = _state2;
            if(state == null) {
                return;
            }
            var (depthTexture, depthView, depthSampler) = CreateDepth(screen);
            state.DepthTexture.Dispose();
            state.DepthTextureView.Dispose();
            state.DepthSampler.Dispose();
            state.DepthTexture = depthTexture;
            state.DepthTextureView = depthView;
            state.DepthSampler = depthSampler;
        };
        screen.Title = "sample";

        var surfaceFormat = screen.SurfaceFormat;
        Debug.WriteLine($"backend: {screen.Backend}");
        var (pixelData, width, height) = SamplePrimitives.LoadImagePixels("happy-tree.png");
        var texture = Texture.Create(screen, new TextureDescriptor
        {
            Size = new Vector3i(width, height, 1),
            MipLevelCount = 1,
            SampleCount = 1,
            Dimension = TextureDimension.D2,
            Format = TextureFormat.Rgba8UnormSrgb,
            Usage = TextureUsages.TextureBinding | TextureUsages.CopyDst,
        });
        texture.Write(0, 4, pixelData);
        var view = TextureView.Create(texture);
        var sampler = Sampler.Create(screen, new SamplerDescriptor
        {
            AddressModeU = AddressMode.ClampToEdge,
            AddressModeV = AddressMode.ClampToEdge,
            AddressModeW = AddressMode.ClampToEdge,
            MagFilter = FilterMode.Linear,
            MinFilter = FilterMode.Nearest,
            MipmapFilter = FilterMode.Nearest,
            AnisotropyClamp = 0,
            LodMaxClamp = 0,
            LodMinClamp = 0,
            BorderColor = null,
            Compare = null,
        });

        var bindGroupLayout = BindGroupLayout.Create(screen, new BindGroupLayoutDescriptor
        {
            Entries = new[]
            {
                BindGroupLayoutEntry.Texture(
                    binding: 0,
                    visibility: ShaderStages.Fragment,
                    type: new TextureBindingData
                    {
                        Multisampled = false,
                        ViewDimension = TextureViewDimension.D2,
                        SampleType = TextureSampleType.FloatFilterable,
                    },
                    count: 0),
                BindGroupLayoutEntry.Sampler(
                    binding: 1,
                    visibility: ShaderStages.Fragment,
                    type: SamplerBindingType.Filtering,
                    count: 0),
            },
        });
        var bindGroup = BindGroup.Create(screen, new BindGroupDescriptor
        {
            Layout = bindGroupLayout,
            Entries = new[]
            {
                BindGroupEntry.TextureView(0, view),
                BindGroupEntry.Sampler(1, sampler),
            },
        });

        var shader = Shader.Create(screen, ShaderSource2);

        var pipelineLayout = PipelineLayout.Create(screen, new PipelineLayoutDescriptor
        {
            BindGroupLayouts = new[]
            {
                bindGroupLayout,
            },
        });

        var screenSize = screen.Size;
        var (depthTexture, depthView, depthSampler) = CreateDepth(screen);

        var renderPipeline = RenderPipeline.Create(screen, new RenderPipelineDescriptor
        {
            Layout = pipelineLayout,
            Vertex = new VertexState
            {
                Module = shader,
                EntryPoint = "vs_main"u8.ToArray(),
                Buffers = new VertexBufferLayout[]
                {
                    new VertexBufferLayout
                    {
                        ArrayStride = (u64)Unsafe.SizeOf<Vertex>(),
                        StepMode = VertexStepMode.Vertex,
                        Attributes = new[]
                        {
                            new VertexAttribute
                            {
                                Offset = 0,
                                ShaderLocation = 0,
                                Format = VertexFormat.Float32x3,
                            },
                            new VertexAttribute
                            {
                                Offset = 12,
                                ShaderLocation = 1,
                                Format = VertexFormat.Float32x2,
                            },
                        },
                    },
                    new VertexBufferLayout
                    {
                        ArrayStride = (u64)Unsafe.SizeOf<InstanceData>(),
                        StepMode = VertexStepMode.Instance,
                        Attributes = new[]
                        {
                            new VertexAttribute
                            {
                                Offset = 0,
                                ShaderLocation = 5,
                                Format = VertexFormat.Float32x3,
                            }
                        },
                    },
                },
            },
            Fragment = new FragmentState
            {
                Module = shader,
                EntryPoint = "fs_main"u8.ToArray(),
                Targets = new ColorTargetState?[]
                {
                    new ColorTargetState
                    {
                        Format = surfaceFormat,
                        Blend = BlendState.Replace,
                        WriteMask = ColorWrites.All,
                    },
                },
            },
            Primitive = new PrimitiveState
            {
                Topology = PrimitiveTopology.TriangleList,
                StripIndexFormat = null,
                FrontFace = FrontFace.Ccw,
                CullMode = Face.Back,
                PolygonMode = PolygonMode.Fill,
            },
            DepthStencil = new DepthStencilState
            {
                Format = depthTexture.Format,
                DepthWriteEnabled = true,
                DepthCompare = CompareFunction.Less,
                Stencil = StencilState.Default,
                Bias = DepthBiasState.Default,
            },
            Multisample = MultisampleState.Default,
            Multiview = 0,
        });

        var (vertices, indices) = SamplePrimitives.SampleData();
        var vb = Buffer.CreateVertexBuffer<Vertex>(screen, vertices);
        var ib = Buffer.CreateIndexBuffer<u16>(screen, indices);
        var indexFormat = IndexFormat.Uint16;

        ReadOnlySpan<InstanceData> instances = stackalloc InstanceData[2]
        {
            new InstanceData(new Vector3(0.5f, 0, 0)),
            new InstanceData(new Vector3(-0.5f, 0, 0)),
        };
        var instanceBuffer = Buffer.CreateVertexBuffer(screen, instances);

        _state2 = new State2
        {
            Texture = texture,
            TextureView = view,
            Sampler = sampler,
            BindGroupLayout = bindGroupLayout,
            BindGroup = bindGroup,
            Shader = shader,
            DepthTexture = depthTexture,
            DepthTextureView = depthView,
            DepthSampler = depthSampler,
            PipelineLayout = pipelineLayout,
            RenderPipeline = renderPipeline,
            VertexBuffer = vb,
            InstanceBuffer = instanceBuffer,
            InstanceCount = instances.Length,
            IndexBuffer = ib,
            IndexCount = indices.Length,
            IndexFormat = indexFormat,
        };
    }

    private static (Texture Depth, TextureView DepthView, Sampler DepthSampler) CreateDepth(IHostScreen screen)
    {
        var screenSize = screen.Size;
        var depth = Texture.Create(screen, new TextureDescriptor
        {
            Size = new Vector3i(screenSize.X, screenSize.Y, 1),
            MipLevelCount = 1,
            SampleCount = 1,
            Dimension = TextureDimension.D2,
            Format = TextureFormat.Depth32Float,
            Usage = TextureUsages.RenderAttachment | TextureUsages.TextureBinding,
        });
        var depthView = TextureView.Create(depth);
        var depthSampler = Sampler.Create(screen, new SamplerDescriptor
        {
            AddressModeU = AddressMode.ClampToEdge,
            AddressModeV = AddressMode.ClampToEdge,
            AddressModeW = AddressMode.ClampToEdge,
            MagFilter = FilterMode.Linear,
            MinFilter = FilterMode.Linear,
            MipmapFilter = FilterMode.Nearest,
            Compare = CompareFunction.LessEqual,
            LodMinClamp = 0,
            LodMaxClamp = 100,
            AnisotropyClamp = 0,
            BorderColor = null,
        });
        return (depth, depthView, depthSampler);
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
                CE.RenderPassDescriptor desc;
                {
                    const int ColorAttachmentCount = 1;
                    var colorAttachments = stackalloc Opt<CE.RenderPassColorAttachment>[ColorAttachmentCount]
                    {
                        new(new CE.RenderPassColorAttachment
                        {
                            view = surfaceTextureView,
                            clear = new Wgpu.Color(0, 0, 0, 0),
                        }),
                    };
                    desc = new CE.RenderPassDescriptor
                    {
                        color_attachments_clear = new() { data = colorAttachments, len = ColorAttachmentCount },
                        depth_stencil_attachment_clear = new(new CE.RenderPassDepthStencilAttachment
                        {
                            view = state.GetDepth().View,
                            depth_clear = Opt<float>.Some(1f),
                            stencil_clear = Opt<uint>.None,
                        }),
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
            entries = Slice.FromFixedSpanUnsafe(stackalloc CE.BindGroupLayoutEntry[1]
            {
                new()
                {
                    binding = 0,
                    visibility = Wgpu.ShaderStages.VERTEX,
                    ty = CE.BindingType.Buffer(UnsafeEx.StackPointer(new CE.BufferBindingData
                    {
                        ty = CE.BufferBindingType.Uniform,
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
            var entries = stackalloc CE.BindGroupEntry[EntryCount]
            {
                new() { binding = 0, resource = CE.BindingResource.Buffer(&bufferBinding), }
            };
            cameraBindGroup = screenRef.CreateBindGroup(new CE.BindGroupDescriptor
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
            var desc = new CE.PipelineLayoutDescriptor(bindGroupLayouts, BindGroupLayoutCount);
            //{
            //    bind_group_layouts = new()
            //    {
            //        data = bindGroupLayouts,
            //        len = BindGroupLayoutCount,
            //    },
            //};
            pipelineLayout = screenRef.CreatePipelineLayout(desc);
        }

        //// Buffer (uniform)
        //var uniformBuffer = HostScreenInitializer.CreateUniformBuffer(screen, stackalloc Vector4[] { new Vector4(1, 0, 0, 1) });

        //// BindGroup (uniform)
        //var uniformBindGroup = HostScreenInitializer.CreateUniformBindGroup(screen, uniformBindGroupLayout, uniformBuffer);

        var depthTextureData = CreateDepthTexture(screenRef, screenSize.Width, screenSize.Height);

        // RenderPipeline
        Box<Wgpu.RenderPipeline> renderPipeline;
        {
            var vertexBufferLayout = new CE.VertexBufferLayout
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
            var instanceBufferLayout = new CE.VertexBufferLayout
            {
                array_stride = (ulong)sizeof(InstanceData),
                step_mode = Wgpu.VertexStepMode.Instance,
                attributes = Slice.FromFixedSpanUnsafe(stackalloc Wgpu.VertexAttribute[]
                {
                    new() { offset = 0, shader_location = 5, format = Wgpu.VertexFormat.Float32x3 },
                }),
            };


            renderPipeline = screenRef.CreateRenderPipeline(new CE.RenderPipelineDescriptor
            {
                layout = pipelineLayout,
                vertex = new()
                {
                    module = shader,
                    entry_point = Slice.FromFixedSpanUnsafe("vs_main"u8),
                    buffers = Slice.FromFixedSpanUnsafe(stackalloc CE.VertexBufferLayout[]
                    {
                        vertexBufferLayout,
                        instanceBufferLayout,
                    }),
                },
                fragment = new Opt<CE.FragmentState>(new CE.FragmentState()
                {
                    module = shader,
                    entry_point = Slice.FromFixedSpanUnsafe("fs_main"u8),
                    targets = Slice.FromFixedSpanUnsafe(stackalloc Opt<CE.ColorTargetState>[]
                        {
                        Opt<CE.ColorTargetState>.Some(new()
                        {
                            format = surfaceFormat,
                            blend = Opt<Wgpu.BlendState>.Some(Wgpu.BlendState.REPLACE),
                            write_mask = Wgpu.ColorWrites.ALL,
                        })
                    }),
                }),
                primitive = new()
                {
                    topology = Wgpu.PrimitiveTopology.TriangleList,
                    strip_index_format = Opt<Wgpu.IndexFormat>.None,
                    front_face = Wgpu.FrontFace.Ccw,
                    cull_mode = Opt<Wgpu.Face>.Some(Wgpu.Face.Back),
                    polygon_mode = Wgpu.PolygonMode.Fill,
                },
                depth_stencil = Opt<CE.DepthStencilState>.Some(new()
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
        const Wgpu.TextureFormat DepthTextureFormat = Wgpu.TextureFormat.Depth32Float;
        var texture = screen.CreateTexture(new CE.TextureDescriptor
        {
            size = new Wgpu.Extent3d
            {
                width = width,
                height = height,
                depth_or_array_layers = 1,
            },
            mip_level_count = 1,
            sample_count = 1,
            dimension = CE.TextureDimension.D2,
            format = DepthTextureFormat,
            usage = Wgpu.TextureUsages.RENDER_ATTACHMENT | Wgpu.TextureUsages.TEXTURE_BINDING,
        });
        var view = texture.AsRef().CreateTextureView();
        var sampler = screen.CreateSampler(new CE.SamplerDescriptor
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

    private unsafe static ReadOnlySpan<byte> ShaderSource2 => """
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

        @vertex fn vs_main(
            v: Vertex,
            instance: InstanceData,
        ) -> VertexOutput {
            var out: VertexOutput;
            out.clip_position = vec4<f32>(v.position + instance.offset, 1.0);
            return out;
        }

        @group(0) @binding(0) var t_diffuse: texture_2d<f32>;
        @group(0)@binding(1) var s_diffuse: sampler;

        @fragment fn fs_main(in: VertexOutput) -> @location(0) vec4<f32> {
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
            width = (uint)width,
            height = (uint)height,
            depth_or_array_layers = 1,
        }; ;
        var texture = screen.CreateTexture(new()
        {
            dimension = CE.TextureDimension.D2,
            format = Wgpu.TextureFormat.Rgba8UnormSrgb,
            mip_level_count = 1,
            sample_count = 1,
            size = size,
            usage = Wgpu.TextureUsages.TEXTURE_BINDING | Wgpu.TextureUsages.COPY_DST,
        });
        fixed(byte* p = pixelBytes) {
            screen.WriteTexture(
                new CE.ImageCopyTexture
                {
                    texture = texture,
                    mip_level = 0,
                    aspect = CE.TextureAspect.All,
                    origin_x = 0,
                    origin_y = 0,
                    origin_z = 0,
                },
                new Slice<byte>(p, pixelBytes.Length),
                new Wgpu.ImageDataLayout
                {
                    offset = 0,
                    bytes_per_row = 4 * (uint)width,
                    rows_per_image = (uint)height,
                },
                size);
        }

        var textureView = texture.AsRef().CreateTextureView();
        var sampler = screen.CreateSampler(new CE.SamplerDescriptor
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
            border_color = Opt<CE.SamplerBorderColor>.None,
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
            entries = Slice.FromFixedSpanUnsafe(stackalloc CE.BindGroupLayoutEntry[2]
            {
                new()
                {
                    binding = 0,
                    visibility = Wgpu.ShaderStages.FRAGMENT,
                    ty = CE.BindingType.Texture(UnsafeEx.StackPointer(new CE.TextureBindingData
                    {
                        multisampled = false,
                        view_dimension = CE.TextureViewDimension.D2,
                        sample_type = CE.TextureSampleType.FloatFilterable,
                    })),
                    count = 0,
                },
                new()
                {
                    binding = 1,
                    visibility = Wgpu.ShaderStages.FRAGMENT,
                    ty = CE.BindingType.Sampler(UnsafeEx.StackPointer(CE.SamplerBindingType.Filtering)),
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
        var entries = stackalloc CE.BindGroupEntry[EntryCount]
        {
            new()
            {
                binding = 0,
                resource = CE.BindingResource.TextureView(textureView),
            },
            new()
            {
                binding = 1,
                resource = CE.BindingResource.Sampler(sampler),
            },
        };

        return screen.CreateBindGroup(new CE.BindGroupDescriptor
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
            entries = Slice.FromFixedSpanUnsafe(stackalloc CE.BindGroupLayoutEntry[1]
                {
                new()
                {
                    binding = 0,
                    visibility = Wgpu.ShaderStages.VERTEX_FRAGMENT,
                    ty = CE.BindingType.Buffer(UnsafeEx.StackPointer(new CE.BufferBindingData
                    {
                        ty = CE.BufferBindingType.Uniform,
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

internal sealed class State2 : IDisposable
{
    public required Texture Texture { get; init; }
    public required TextureView TextureView { get; init; }
    public required Sampler Sampler { get; init; }
    public required BindGroupLayout BindGroupLayout { get; init; }
    public required BindGroup BindGroup { get; init; }
    public required Shader Shader { get; init; }
    public required PipelineLayout PipelineLayout { get; init; }
    public required RenderPipeline RenderPipeline { get; init; }

    public required Buffer VertexBuffer { get; init; }
    public required Buffer InstanceBuffer { get; init; }
    public required int InstanceCount { get; init; }
    public required Buffer IndexBuffer { get; init; }
    public required int IndexCount { get; init; }
    public required IndexFormat IndexFormat { get; init; }


    public required Texture DepthTexture { get; set; }
    public required TextureView DepthTextureView { get; set; }
    public required Sampler DepthSampler { get; set; }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}

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
    Wgpu.TextureFormat Format);


internal unsafe static class UnsafeEx
{
    public static T* StackPointer<T>(in T x) where T : unmanaged
    {
        return (T*)Unsafe.AsPointer(ref Unsafe.AsRef(in x));
    }
}
