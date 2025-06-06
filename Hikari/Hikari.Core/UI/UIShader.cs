#nullable enable
using Hikari.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Utf8StringInterpolation;

namespace Hikari.UI;

internal sealed class UIShader : ITypedShader
{
    private static readonly ConcurrentDictionary<Screen, Own<Texture2D>> _textureCache = new();
    private static readonly ConcurrentDictionary<Screen, Own<Sampler>> _samplerCache = new();
    private static readonly ConcurrentDictionary<Screen, Own<UIShader>> _shaderCache = new();

    private static readonly Lazy<ImmutableArray<byte>> _defaultShaderSource = new(() =>
    {
        var fs_main = """
            fn calc_back_color(
                f_pos: vec2<f32>,
                pos: vec2<f32>,
                size: vec2<f32>,
            ) -> vec4<f32> {
                let texel_color = get_texel_color(f_pos, TEXT_HALIGN_CENTER, TEXT_VALIGN_CENTER, pos, size);
                let bg_color = calc_background_brush_color(f_pos, pos, size);
                return blend(texel_color, bg_color);
            }

            @fragment fn fs_main(
                f: V2F,
            ) -> @location(0) vec4<f32> {
                return gamma22(ui_color_shared_algorithm(f));
            }
            """u8;

        using var bw = new PooledArrayBufferWriter<byte>();
        using(var sb = Utf8String.CreateWriter(bw)) {
            sb.AppendUtf8(UIShaderSource.TypeDef); sb.AppendLine();
            sb.AppendUtf8(UIShaderSource.ConstDef); sb.AppendLine();
            sb.AppendUtf8(UIShaderSource.Group0); sb.AppendLine();
            sb.AppendUtf8(UIShaderSource.Group1); sb.AppendLine();
            sb.AppendUtf8(UIShaderSource.Group2); sb.AppendLine();
            sb.AppendUtf8(UIShaderSource.Fn_pow_x2); sb.AppendLine();
            sb.AppendUtf8(UIShaderSource.Fn_blend); sb.AppendLine();
            sb.AppendUtf8(UIShaderSource.Fn_vs_main); sb.AppendLine();
            sb.AppendUtf8(UIShaderSource.Fn_corner_area_color); sb.AppendLine();
            sb.AppendUtf8(UIShaderSource.Fn_get_texel_color); sb.AppendLine();
            sb.AppendUtf8(UIShaderSource.Fn_calc_background_brush_color); sb.AppendLine();
            sb.AppendUtf8(UIShaderSource.Fn_ui_color_shared_algorithm); sb.AppendLine();
            sb.AppendUtf8(UIShaderSource.Fn_gamma22); sb.AppendLine();
            sb.AppendUtf8(fs_main);
        }
        return bw.WrittenSpan.ToImmutableArray();
    });

    private readonly Shader _shader;
    private readonly DisposableBag _disposables;

    public Screen Screen => _shader.Screen;

    public ImmutableArray<ShaderPassData> ShaderPasses => _shader.ShaderPasses;

    private UIShader(Screen screen)
    {
        var disposables = new DisposableBag();
        var shader = Shader.Create(
            screen,
            [
                new()
                {
                    Source = _defaultShaderSource.Value,
                    LayoutDescriptor = PipelineLayoutFactory(screen, disposables),
                    PipelineDescriptorFactory = (module, layout) => PipelineFactory(module, layout, screen.Surface.Format, screen.DepthStencil.Format),
                    PassKind = PassKind.Forward,
                    SortOrderInPass = 10000,
                    OnRenderPass = static (in RenderPassState state) =>
                    {
                        state.RenderPass.SetPipeline(state.Pipeline);
                        state.Material.SetBindGroupsTo(state.RenderPass, state.PassIndex, state.Renderer);
                        state.RenderPass.SetVertexBuffer(0, state.Mesh.VertexBuffer);
                        state.RenderPass.SetIndexBuffer(state.Mesh.IndexBuffer, state.Mesh.IndexFormat);
                        state.RenderPass.DrawIndexed(state.Submesh.IndexOffset, state.Submesh.IndexCount, state.Submesh.VertexOffset, 0, 1);
                    },
                },
            ]).AddTo(disposables);
        _shader = shader;
        _disposables = disposables;
    }

    private void Release()
    {
        _disposables.Dispose();
    }

    private static Own<UIShader> Create(Screen screen)
    {
        return Own.New(new UIShader(screen), static s => SafeCast.As<UIShader>(s).Release());
    }

    public static Texture2D GetEmptyTexture2D(Screen screen)
    {
        return _textureCache.GetOrAdd(screen, static screen =>
        {
            var texture = Texture2D.Create(screen, new()
            {
                Size = new(1, 1),
                Format = TextureFormat.Rgba8Unorm,
                MipLevelCount = 1,
                Usage = TextureUsages.TextureBinding | TextureUsages.CopySrc,
                SampleCount = 1,
            });
            screen.Closed.Subscribe(static screen =>
            {
                if(_textureCache.TryRemove(screen, out var texture)) {
                    texture.Dispose();
                }
            });
            return texture;
        }).AsValue();
    }

    public static Sampler GetEmptySampler(Screen screen)
    {
        return _samplerCache.GetOrAdd(screen, static screen =>
        {
            var sampler = Sampler.Create(screen, new()
            {
                AddressModeU = AddressMode.ClampToEdge,
                AddressModeV = AddressMode.ClampToEdge,
                AddressModeW = AddressMode.ClampToEdge,
                MinFilter = FilterMode.Nearest,
                MagFilter = FilterMode.Nearest,
                MipmapFilter = FilterMode.Nearest,
            });
            screen.Closed.Subscribe(static screen =>
            {
                if(_samplerCache.TryRemove(screen, out var sampler)) {
                    sampler.Dispose();
                }
            });
            return sampler;
        }).AsValue();
    }

    public static UIShader CreateOrCached(Screen screen)
    {
        return _shaderCache.GetOrAdd(screen, static screen =>
        {
            var shader = Create(screen);
            screen.Closed.Subscribe(static screen =>
            {
                if(_shaderCache.TryRemove(screen, out var shader)) {
                    shader.Dispose();
                }
            });
            return shader;
        }).AsValue();
    }


    private static PipelineLayoutDescriptor PipelineLayoutFactory(
        Screen screen,
        DisposableBag disposables)
    {
        return new()
        {
            BindGroupLayouts =
            [
                BindGroupLayout.Create(screen, new()
                {
                    Entries =
                    [
                        BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex | ShaderStages.Fragment, new BufferBindingData { Type = BufferBindingType.Uniform }),
                        BindGroupLayoutEntry.Buffer(1, ShaderStages.Vertex | ShaderStages.Fragment, new BufferBindingData { Type = BufferBindingType.Uniform }),
                    ],
                }).AddTo(disposables),
                BindGroupLayout.Create(screen, new()
                {
                    Entries =
                    [
                        BindGroupLayoutEntry.Texture(0, ShaderStages.Vertex | ShaderStages.Fragment, new TextureBindingData
                        {
                            ViewDimension = TextureViewDimension.D2,
                            Multisampled = false,
                            SampleType = TextureSampleType.FloatNotFilterable,
                        }),
                        BindGroupLayoutEntry.Sampler(1, ShaderStages.Vertex | ShaderStages.Fragment, SamplerBindingType.NonFiltering),
                        BindGroupLayoutEntry.Buffer(2, ShaderStages.Vertex | ShaderStages.Fragment, new BufferBindingData { Type = BufferBindingType.Uniform }),
                    ],
                }).AddTo(disposables),
                BindGroupLayout.Create(screen, new()
                {
                    Entries =
                    [
                        BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex | ShaderStages.Fragment, new BufferBindingData { Type = BufferBindingType.StorageReadOnly }),
                    ],
                }).AddTo(disposables),
            ],
        };
    }

    private static RenderPipelineDescriptor PipelineFactory(ShaderModule module, PipelineLayout layout, TextureFormat surfaceFormat, TextureFormat depthStencilFormat)
    {
        return new RenderPipelineDescriptor
        {
            Layout = layout,
            Vertex = new VertexState()
            {
                Module = module,
                EntryPoint = "vs_main"u8.ToImmutableArray(),
                Buffers =
                [
                    VertexBufferLayout.FromVertex<VertexSlim>(
                    [
                        (0, VertexFieldSemantics.Position),
                        (1, VertexFieldSemantics.UV),
                    ]),
                ],
            },
            Fragment = new FragmentState()
            {
                Module = module,
                EntryPoint = "fs_main"u8.ToImmutableArray(),
                Targets =
                [
                    new ColorTargetState
                    {
                        Format = surfaceFormat,
                        Blend = BlendState.AlphaBlending,
                        WriteMask = ColorWrites.All,
                    },
                ],
            },
            Primitive = new PrimitiveState()
            {
                Topology = PrimitiveTopology.TriangleList,
                FrontFace = FrontFace.Ccw,
                CullMode = Face.Back,
                PolygonMode = PolygonMode.Fill,
                StripIndexFormat = null,
            },
            DepthStencil = new DepthStencilState
            {
                Format = depthStencilFormat,
                DepthWriteEnabled = false,
                DepthCompare = CompareFunction.Always,
                Stencil = StencilState.Default,
                Bias = DepthBiasState.Default,
            },
            Multisample = MultisampleState.Default,
            Multiview = 0,
        };
    }
}
