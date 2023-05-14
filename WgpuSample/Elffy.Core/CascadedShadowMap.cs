#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy;

public sealed class CascadedShadowMap<TVertex> where TVertex : unmanaged, IVertex
{
    private readonly DirectionalLight _light;

    internal CascadedShadowMap(DirectionalLight light)
    {
        _light = light;
        var screen = light.Screen;

        var size = screen.ClientSize;
        var depth = Texture.Create(screen, new TextureDescriptor
        {
            Dimension = TextureDimension.D2,
            Format = TextureFormat.Depth32Float,
            MipLevelCount = 1,
            SampleCount = 1,
            Usage = TextureUsages.RenderAttachment | TextureUsages.TextureBinding | TextureUsages.StorageBinding,
            Size = new Vector3u(size.X, size.Y, 1),
        });
        var sampler = Sampler.NoMipmap(screen, AddressMode.ClampToEdge, FilterMode.Nearest, FilterMode.Nearest);
    }

    private static ReadOnlySpan<byte> ShadowMappingSource => """
        struct Scene {
            lightViewProjMatrix: mat4x4<f32>,
            cameraViewProjMatrix: mat4x4<f32>,
            lightPos: vec3<f32>,
        }

        struct Model {
            modelMatrix: mat4x4<f32>,
        }

        struct Vertex {
            @location(0) pos: vec3<f32>,
        }

        @group(0) @binding(0) var<uniform> scene: Scene;
        @group(1) @binding(0) var<uniform> model: Model;

        @vertex fn vs_main(
            v: Vertex,
        ) -> @builtin(position) vec4<f32> {
            return scene.lightViewProjMatrix * model.modelMatrix * vec4(pos, 1.0);
        }
        """u8;

    private static Own<RenderPipeline> CreateShadowPipeline(
        Screen screen,
        PipelineLayout pipelineLayout,
        out Own<ShaderModule> shaderModule)
    {
        shaderModule = ShaderModule.Create(screen, ShadowMappingSource);
        return RenderPipeline.Create(screen, new RenderPipelineDescriptor
        {
            Layout = pipelineLayout,
            Vertex = new VertexState
            {
                Module = shaderModule.AsValue(),
                EntryPoint = "vs_main"u8.ToArray(),
                Buffers = new VertexBufferLayout[]
                {
                    new VertexBufferLayout
                    {
                        ArrayStride = TVertex.VertexSize,
                        StepMode = VertexStepMode.Vertex,
                        Attributes = new VertexAttr[1]
                        {
                            new VertexAttr
                            {
                                Format = VertexFormat.Float32x3,
                                Offset = TVertex.PositionOffset,
                                ShaderLocation = 0,
                            },
                        },
                    },
                },
            },
            Fragment = null,
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
                Format = screen.DepthTexture.Format,
                DepthWriteEnabled = true,
                DepthCompare = CompareFunction.Less,
                Stencil = StencilState.Default,
                Bias = DepthBiasState.Default,
            },
            Multisample = MultisampleState.Default,
            Multiview = 0,
        });
    }

    private Own<RenderPipeline> _shadowPipeline;

    private void RenderShadowMap(RenderPass pass, Mesh<TVertex> mesh)
    {
        var pipeline = _shadowPipeline.AsValue();
        pass.SetPipeline(pipeline);
        pass.SetVertexBuffer(0, mesh.VertexBuffer);
        pass.SetIndexBuffer(mesh.IndexBuffer);
        //pass.SetBindGroup(0, material.BindGroup0);
        //pass.SetBindGroup(1, material.BindGroup1);
        //pass.SetBindGroup(2, material.BindGroup2);
        pass.DrawIndexed(0, mesh.IndexCount, 0, 0, 1);
    }
}


//public sealed class ShadowMapping<TLayer, TVertex, TShader, TMaterial>
//    : ComputeOperation
//    where TLayer : ObjectLayer<TLayer, TVertex, TShader, TMaterial>
//    where TVertex : unmanaged, IVertex
//    where TShader : Shader<TShader, TMaterial>
//    where TMaterial : Material<TMaterial, TShader>
//{
//    private readonly ObjectLayer<TLayer, TVertex, TShader, TMaterial> _layer;

//    public ShadowMapping(
//        ObjectLayer<TLayer, TVertex, TShader, TMaterial> layer,
//        Screen screen,
//        int sortOrder)
//        : base(screen, sortOrder, Init(screen, out var disposables))
//    {
//        _layer = layer;
//    }

//    private static ComputePipelineDescriptor Init(Screen screen, out IDisposable[] disposables)
//    {
//        var bgl = BindGroupLayout.Create(screen, new()
//        {
//            Entries = new BindGroupLayoutEntry[]
//            {
//                BindGroupLayoutEntry.Buffer(0, ShaderStages.Compute, new()
//                {
//                    Type = BufferBindingType.StorateReadOnly,
//                    MinBindingSize = null,
//                    HasDynamicOffset = false,
//                }),
//            }
//        });

//        var layout = PipelineLayout.Create(screen, new()
//        {
//            BindGroupLayouts = new BindGroupLayout[]
//            {
//                bgl.AsValue(),
//                bgl.AsValue(),
//            },
//        });
//        var shaderModule = ShaderModule.Create(screen, ShadowMappingSource);

//        var d = new ComputePipelineDescriptor
//        {
//            Layout = layout.AsValue(),
//            Module = shaderModule.AsValue(),
//            EntryPoint = "main"u8.ToArray(),
//        };
//        disposables = new IDisposable[]
//        {
//            bgl,
//            layout,
//            shaderModule
//        };
//        return d;
//    }

//    private static ReadOnlySpan<byte> ShadowMappingSource => """
//        // TODO:
//        struct Scene {
//            lightViewProjMatrix: mat4x4<f32>,
//            cameraViewProjMatrix: mat4x4<f32>,
//            lightPos: vec3<f32>,
//        }

//        struct Model {
//            modelMatrix: mat4x4<f32>,
//        }

//        struct Vertex {
//            @location(0) pos: vec3<f32>,
//        }

//        @group(0) @binding(0) var<uniform> scene: Scene;
//        @group(1) @binding(0) var<uniform> model: Model;

//        @vertex fn vs_main(
//            v: Vertex,
//        ) -> @builtin(position) vec4<f32> {
//            return scene.lightViewProjMatrix * model.modelMatrix * vec4(pos, 1.0);
//        }
//        """u8;

//    protected override void Execute(in ComputePass pass, ComputePipeline pipeline)
//    {
//        //_layer.InvokeRender
//        pass.SetPipeline(pipeline);
//        //pass.SetBindGroup(0, );
//        //pass.DispatchWorkgroups()
//    }
//}
