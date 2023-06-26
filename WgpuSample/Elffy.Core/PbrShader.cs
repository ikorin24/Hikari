#nullable enable
using System;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy;

public sealed class PbrShader : Shader<PbrShader, PbrMaterial>
{
    private static T ShadowShaderSource<TArg, T>(uint cascade, TArg arg, ReadOnlySpanFunc<byte, TArg, T> func)
    {
        using var builder = new Utf8StringBuilder();
        builder.Append("const CASCADE: u32 = "u8);
        builder.Append(cascade);
        builder.Append("""
        u;
        @group(0) @binding(0) var<uniform> model: mat4x4<f32>;
        @group(0) @binding(1) var<storage, read> lightMatrices: array<mat4x4<f32>>;
        @vertex fn vs_main(
            @location(0) pos: vec3<f32>,
        ) -> @builtin(position) vec4<f32> {
            return lightMatrices[CASCADE] * model * vec4<f32>(pos, 1.0);
        }
        """u8);
        return func(builder.Utf8String, arg);
    }

    private static ReadOnlySpan<byte> ShaderSource => """
        struct Vin {
            @location(0) pos: vec3<f32>,
            @location(1) normal: vec3<f32>,
            @location(2) uv: vec2<f32>,
            @location(3) tangent: vec3<f32>,
        }
        struct V2F {
            @builtin(position) clip_pos: vec4<f32>,
            @location(0) pos_camera_coord: vec3<f32>,
            @location(1) uv: vec2<f32>,
            @location(2) tangent_camera_coord: vec3<f32>,
            @location(3) bitangent_camera_coord: vec3<f32>,
            @location(4) normal_camera_coord: vec3<f32>,
        }

        struct GBuffer {
            @location(0) g0 : vec4<f32>,
            @location(1) g1 : vec4<f32>,
            @location(2) g2 : vec4<f32>,
            @location(3) g3 : vec4<f32>,
        }
        struct UniformValue {
            model: mat4x4<f32>,
        }

        struct CameraMatrix {
            proj: mat4x4<f32>,
            view: mat4x4<f32>,
            inv_proj: mat4x4<f32>,
            inv_view: mat4x4<f32>,
        }

        @group(0) @binding(0) var<uniform> u: UniformValue;
        @group(0) @binding(1) var tex_sampler: sampler;
        @group(0) @binding(2) var albedo_tex: texture_2d<f32>;
        @group(0) @binding(3) var mr_tex: texture_2d<f32>;
        @group(0) @binding(4) var normal_tex: texture_2d<f32>;
        @group(1) @binding(0) var<uniform> c: CameraMatrix;

        @vertex fn vs_main(
            v: Vin,
        ) -> V2F {
            let model_view = c.view * u.model;
            let mv33 = mat44_to_33(model_view);
            var output: V2F;
            let pos4: vec4<f32> = model_view * vec4(v.pos, 1.0);

            output.clip_pos = c.proj * pos4;
            output.pos_camera_coord = pos4.xyz / pos4.w;
            output.uv = v.uv;
            output.tangent_camera_coord = normalize(mv33 * v.tangent);
            output.bitangent_camera_coord = normalize(mv33 * cross(v.normal, v.tangent));
            output.normal_camera_coord = normalize(mv33 * v.normal);
            return output;
        }

        @fragment fn fs_main(in: V2F) -> GBuffer {
            // TBN matrix: tangent space -> camera space
            var tbn = mat3x3<f32>(in.tangent_camera_coord, in.bitangent_camera_coord, in.normal_camera_coord);
            var mrao: vec3<f32> = textureSample(mr_tex, tex_sampler, in.uv).rgb;
            var normal_camera_coord: vec3<f32> = tbn * (textureSample(normal_tex, tex_sampler, in.uv).rgb * 2.0 - 1.0);

            var output: GBuffer;
            output.g0 = vec4(in.pos_camera_coord, mrao.r);
            output.g1 = vec4(normal_camera_coord, mrao.g);
            output.g2 = textureSample(albedo_tex, tex_sampler, in.uv);
            output.g3 = vec4(mrao.b, 1.0, 1.0, 1.0);
            return output;
        }

        fn mat44_to_33(m: mat4x4<f32>) -> mat3x3<f32> {
            return mat3x3<f32>(m[0].xyz, m[1].xyz, m[2].xyz);
        }
        """u8;

    private readonly Own<ShaderModule>[] _shadowModules;
    private readonly Own<PipelineLayout> _shadowPipelineLayout;
    private readonly Own<BindGroupLayout> _bindGroupLayout0;
    private readonly BindGroupLayout _bindGroupLayout1;

    private readonly Own<BindGroupLayout> _shadowBindGroupLayout0;

    public BindGroupLayout BindGroupLayout0 => _bindGroupLayout0.AsValue();

    public BindGroupLayout BindGroupLayout1 => _bindGroupLayout1;

    public BindGroupLayout ShadowBindGroupLayout0 => _shadowBindGroupLayout0.AsValue();

    public ShaderModule ShadowModule(uint cascade) => _shadowModules[cascade].AsValue();
    public PipelineLayout ShadowPipelineLayout => _shadowPipelineLayout.AsValue();

    private PbrShader(Screen screen)
        : base(
            screen,
            ShaderSource,
            BuildPipelineLayoutDescriptor(
                screen,
                out var bindGroupLayout0,
                out var bindGroupLayout1))
    {
        _bindGroupLayout0 = bindGroupLayout0;
        _bindGroupLayout1 = bindGroupLayout1;

        var cascadeCount = screen.Lights.DirectionalLight.CascadeCount;
        var shadowModules = new Own<ShaderModule>[cascadeCount];
        for(uint i = 0; i < shadowModules.Length; i++) {
            shadowModules[i] = ShadowShaderSource(i, screen, (source, screen) => ShaderModule.Create(screen, source));
        }
        _shadowModules = shadowModules;
        _shadowPipelineLayout = BuildShadowPipeline(screen, out var shadowBgl0);
        _shadowBindGroupLayout0 = shadowBgl0;
    }

    public override void Validate()
    {
        base.Validate();
        _bindGroupLayout1.Validate();
    }

    public static Own<PbrShader> Create(Screen screen)
    {
        var self = new PbrShader(screen);
        return CreateOwn(self);
    }

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        if(manualRelease) {
            _bindGroupLayout0.Dispose();
            _shadowBindGroupLayout0.Dispose();
            foreach(var item in _shadowModules) {
                item.Dispose();
            }
            _shadowPipelineLayout.Dispose();
        }
    }

    private static PipelineLayoutDescriptor BuildPipelineLayoutDescriptor(
        Screen screen,
        out Own<BindGroupLayout> bindGroupLayout0,
        out BindGroupLayout bindGroupLayout1)
    {
        bindGroupLayout1 = screen.Camera.CameraDataBindGroupLayout;
        return new PipelineLayoutDescriptor
        {
            BindGroupLayouts = new[]
            {
                BindGroupLayout.Create(screen, new()
                {
                    Entries = new[]
                    {
                        BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex, new BufferBindingData
                        {
                            Type = BufferBindingType.Uniform,
                        }),
                        BindGroupLayoutEntry.Sampler(1, ShaderStages.Fragment, SamplerBindingType.Filtering),
                        BindGroupLayoutEntry.Texture(2, ShaderStages.Fragment, new TextureBindingData
                        {
                            ViewDimension = TextureViewDimension.D2,
                            Multisampled = false,
                            SampleType = TextureSampleType.FloatFilterable,
                        }),
                        BindGroupLayoutEntry.Texture(3, ShaderStages.Fragment, new TextureBindingData
                        {
                            ViewDimension = TextureViewDimension.D2,
                            Multisampled = false,
                            SampleType = TextureSampleType.FloatFilterable,
                        }),
                        BindGroupLayoutEntry.Texture(4, ShaderStages.Fragment, new TextureBindingData
                        {
                            ViewDimension = TextureViewDimension.D2,
                            Multisampled = false,
                            SampleType = TextureSampleType.FloatFilterable,
                        }),
                    },
                }).AsValue(out bindGroupLayout0),
                bindGroupLayout1,
            },
        };
    }

    private static Own<PipelineLayout> BuildShadowPipeline(
        Screen screen,
        out Own<BindGroupLayout> bgl0)
    {
        return PipelineLayout.Create(screen, new()
        {
            BindGroupLayouts = new[]
            {
                BindGroupLayout.Create(screen, new()
                {
                    Entries = new[]
                    {
                        BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex, new() { Type = BufferBindingType.Uniform }),
                        BindGroupLayoutEntry.Buffer(1, ShaderStages.Vertex, new() { Type = BufferBindingType.StorateReadOnly }),
                    },
                }).AsValue(out bgl0),
            },
        });
    }
}

file ref struct Utf8StringBuilder
{
    private byte[] _buffer;
    private int _length;

    public ReadOnlySpan<byte> Utf8String => _buffer.AsSpan(0, _length);

    public Utf8StringBuilder()
    {
        _buffer = ArrayPool<byte>.Shared.Rent(0);
    }

    public Utf8StringBuilder(int capacity = 0)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(capacity);
    }

    public void AppendLine(ReadOnlySpan<byte> utf8String)
    {
        Append(utf8String);
        if(OperatingSystem.IsWindows()) {
            Append("\r\n"u8);
        }
        else {
            Append("\n"u8);
        }
    }

    [SkipLocalsInit]
    public void Append(uint value)
    {
        Span<byte> buf = stackalloc byte[10];
        var result = Utf8Formatter.TryFormat(value, buf, out var writtenLen);
        Debug.Assert(result);
        Append(buf.Slice(0, writtenLen));
    }

    public void Append(scoped ReadOnlySpan<byte> utf8String)
    {
        if(utf8String.Length > _buffer.Length - _length) {
            var c = int.Max(_length + utf8String.Length, checked(_buffer.Length * 2));
            var newBuffer = ArrayPool<byte>.Shared.Rent(c);
            _buffer.AsSpan(0, _length).CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = newBuffer;
        }
        Debug.Assert(_buffer.Length - _length >= utf8String.Length);
        utf8String.CopyTo(_buffer.AsSpan(_length));
        _length += utf8String.Length;
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_buffer);
    }
}
