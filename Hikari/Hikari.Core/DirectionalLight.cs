#nullable enable
using Cysharp.Threading.Tasks;
using Hikari.Internal;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikari;

public sealed partial class DirectionalLight
{
    internal const uint CascadeCountConst = 4;       // 2 ~
    internal const u32 ShadowMapWidth = 1024;
    internal const u32 ShadowMapHeight = ShadowMapWidth;
    internal const TextureFormat ShadowMapFormat = TextureFormat.Depth32Float;

    private readonly Screen _screen;
    private readonly CachedOwnBuffer<DirectionalLightData> _lightData;
    private TypedOwnBuffer<LightMatrixArray> _lightMatrices;
    private TypedOwnBuffer<CascadeFarArray> _cascadeFars;
    private readonly Own<Texture2D> _shadowMap;
    private readonly Own<Texture2D> _depthOnRenderShadowMap;
    private readonly BindGroup _renderShadowBindGroup;
    private readonly BindGroup _shadowMapBindGroup;
    private readonly DisposableBag _disposables;
    private readonly SubscriptionBag _subscriptionBag = new();

    public Screen Screen => _screen;

    public Texture2D ShadowMap => _shadowMap.AsValue();

    public uint CascadeCount => CascadeCountConst;

    public Vector3 Direction => _lightData.Data.Direction;

    public Color3 Color => _lightData.Data.Color;

    public BindGroup RenderShadowBindGroup => _renderShadowBindGroup;
    public BindGroup ShadowMapBindGroup => _shadowMapBindGroup;

    internal Buffer DataBuffer => _lightData.AsBuffer();

    public ImmutableArray<RenderPassDefinition> ShadowMapPassDefinitions { get; } = [
        new RenderPassDefinition
        {
            Kind = PassKind.ShadowMap,
            Factory = static (screen, _) =>
            {
                return RenderPass.Create(
                    screen,
                    [],
                    new DepthStencilAttachment
                    {
                        Target = screen.Lights.DirectionalLight.ShadowMap,
                        LoadOp = new DepthStencilBufferLoadOp
                        {
                            Depth = DepthBufferLoadOp.Clear(0f),
                            Stencil = null,
                        },
                    });
            },
        },
    ];

    public void SetLightData(in Vector3 direction, in Color3 color)
    {
        direction.Normalize();
        if(direction.ContainsNaNOrInfinity) {
            throw new ArgumentException(nameof(direction));
        }
        _lightData.WriteData(new DirectionalLightData(direction, color));
        UpdateLightMatrix();
    }

    internal DirectionalLight(Screen screen)
    {
        var data = new DirectionalLightData(
            new Vector3(0f, -1f, -0.3f).Normalized(),
            new Color3(1, 1, 1)
        );
        _screen = screen;
        _lightData = new(screen, in data, BufferUsages.Storage | BufferUsages.Uniform | BufferUsages.CopyDst);
        _shadowMap = Texture2D.Create(screen, new()
        {
            Size = new Vector2u(ShadowMapWidth * CascadeCountConst, ShadowMapHeight),
            Format = ShadowMapFormat,
            MipLevelCount = 1,
            SampleCount = 1,
            Usage = TextureUsages.TextureBinding | TextureUsages.RenderAttachment | TextureUsages.CopySrc,
        });
        _depthOnRenderShadowMap = Texture2D.Create(screen, new()
        {
            Size = new Vector2u(ShadowMapWidth * CascadeCountConst, ShadowMapHeight),
            Format = TextureFormat.Depth32Float,
            MipLevelCount = 1,
            SampleCount = 1,
            Usage = TextureUsages.TextureBinding | TextureUsages.RenderAttachment | TextureUsages.CopySrc,
        });
        _lightMatrices = new TypedOwnBuffer<LightMatrixArray>(
            screen,
            LightMatrixArray.IdentityArray,
            BufferUsages.Storage | BufferUsages.Uniform | BufferUsages.CopyDst);
        _cascadeFars = new TypedOwnBuffer<CascadeFarArray>(
            screen,
            default,
            BufferUsages.Storage | BufferUsages.Uniform | BufferUsages.CopyDst);
        screen.Camera.MatrixChanged.Subscribe(_ => UpdateLightMatrix()).AddTo(_subscriptionBag);
        UpdateLightMatrix();

        var disposables = new DisposableBag();
        _renderShadowBindGroup = BindGroup.Create(screen, new()
        {
            Layout = BindGroupLayout.Create(screen, new()
            {
                Entries =
                [
                    BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex, new() { Type = BufferBindingType.StorageReadOnly }),
                ],
            }).AddTo(disposables),
            Entries =
            [
                BindGroupEntry.Buffer(0, _lightMatrices.AsBuffer()),
            ],
        }).AddTo(disposables);

        _shadowMapBindGroup = BindGroup.Create(screen, new()
        {
            Layout = BindGroupLayout.Create(screen, new()
            {
                Entries =
                [
                    BindGroupLayoutEntry.Texture(0, ShaderStages.Fragment, new()
                    {
                        ViewDimension = TextureViewDimension.D2,
                        Multisampled = false,
                        SampleType = TextureSampleType.Depth,
                    }),
                    BindGroupLayoutEntry.Buffer(1, ShaderStages.Fragment, new() { Type = BufferBindingType.StorageReadOnly }),
                    BindGroupLayoutEntry.Buffer(2, ShaderStages.Fragment, new() { Type = BufferBindingType.StorageReadOnly }),
                ],
            }).AddTo(disposables),
            Entries =
            [
                BindGroupEntry.TextureView(0, ShadowMap.View),
                BindGroupEntry.Buffer(1, _lightMatrices.AsBuffer()),
                BindGroupEntry.Buffer(2, _cascadeFars.AsBuffer()),
            ],
        }).AddTo(disposables);
        _disposables = disposables;
    }

    internal void DisposeInternal()
    {
        _lightData.Dispose();
        _shadowMap.Dispose();
        _depthOnRenderShadowMap.Dispose();
        _lightMatrices.Dispose();
        _cascadeFars.Dispose();
        _subscriptionBag.Dispose();
        _disposables.Dispose();
    }

    [SkipLocalsInit]
    internal void UpdateLightMatrix()
    {
        var camera = _screen.Camera;
        var lightDirection = Direction;
        CalcLightMatrix(lightDirection, camera.ReadState(), out var lightMatrices, out var cascadeFars);
        _lightMatrices.WriteData(in lightMatrices);
        _cascadeFars.WriteData(in cascadeFars);
    }

    private static void CalcLightMatrix(in Vector3 lightDir, in Camera.CameraState camera, out LightMatrixArray lightMatrixArray, out CascadeFarArray cascadeFarArray)
    {
        Vector3 lUp;
        {
            var dirX0Z = new Vector3(lightDir.X, 0, lightDir.Z).Normalized();
            lUp = dirX0Z.ContainsNaNOrInfinity ?
                Vector3.UnitX :
                Quaternion.FromTwoVectors(dirX0Z, lightDir) * Vector3.UnitY;
        }

        var lightMatrices = lightMatrixArray.AsSpan();
        var cascadeFars = cascadeFarArray.AsSpan().Slice(0, lightMatrices.Length);
        var depthRange = cascadeFarArray.AsSpan().Slice(lightMatrices.Length);

        const float MaxShadowMapFar = 100;

        float logNear = float.Log(camera.Near);
        float logFar = float.Log(MaxShadowMapFar);
        float logStep = (logFar - logNear) / lightMatrices.Length;

        for(int i = 0; i < lightMatrices.Length; i++) {
            var n = float.Exp(logNear + i * logStep);
            var f = float.Exp(logNear + (i + 1) * logStep);
            cascadeFars[i] = f;

            var cameraDir = camera.GetDirection();
            var cascadeNearPoint = camera.Position + cameraDir * n;
            var cascadeFarPoint = camera.Position + cameraDir * f;
            var cascadedCenter = (cascadeNearPoint + cascadeFarPoint) / 2;

            if(camera.ProjectionMode.IsPerspective(out var fovy)) {
                var fovx = 2f * float.Atan(camera.Aspect * float.Tan(fovy / 2f));
                var cascadedProj = Matrix4.ReversedZ.PerspectiveProjection(fovy, camera.Aspect, n * float.Cos(fovx * 0.5f), f);
                Frustum.FromMatrix(cascadedProj, camera.Matrix.View, out var cascadedFrustum);
                var lview = Matrix4.LookAt(cascadedCenter - lightDir, cascadedCenter, lUp);

                var min = Vector3.MaxValue;
                var max = Vector3.MinValue;
                foreach(var corner in cascadedFrustum.Corners) {
                    var p = lview.Transform(corner);
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }
                var aabbInLightSpace = Bounds.FromMinMax(min, max);

                var depthNear = -(aabbInLightSpace.Max.Z + aabbInLightSpace.Size.Z * 5.0f);  // TODO:
                var depthFar = -aabbInLightSpace.Min.Z;
                depthRange[i] = depthFar - depthNear;
                var lproj = Matrix4.ReversedZ.OrthographicProjection(
                aabbInLightSpace.Min.X, aabbInLightSpace.Max.X,
                aabbInLightSpace.Min.Y, aabbInLightSpace.Max.Y,
                depthNear,
                depthFar);
                lightMatrices[i] = lproj * lview;
            }
            else if(camera.ProjectionMode.IsOrthographic(out var height)) {
                throw new NotImplementedException();        // TODO:
            }
            else {
                throw new NotSupportedException();
            }
        }
    }

    [BufferDataStruct]
    private readonly partial struct DirectionalLightData
    {
        [FieldOffset(OffsetOf._direction)]
        private readonly Vector4 _direction;    // (x, y, z, _)
        [FieldOffset(OffsetOf._color)]
        private readonly Color4 _color;        // (r, g, b, _)

        public Vector3 Direction => _direction.Xyz;
        public Color3 Color => _color.ToColor3();

        public DirectionalLightData(in Vector3 direction, in Color3 color)
        {
            _direction = new Vector4(direction, 0);
            _color = new Color4(color, 0);
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = sizeof(float) * 16 * (int)CascadeCountConst)]
    private unsafe partial struct LightMatrixArray
    {
        [FieldOffset(0)]
        private fixed float _array[16 * (int)CascadeCountConst];

        public static LightMatrixArray IdentityArray
        {
            get
            {
                var instance = default(LightMatrixArray);
                ref var head = ref instance.Head;
                for(int i = 0; i < CascadeCountConst; i++) {
                    Unsafe.Add(ref head, i) = Matrix4.Identity;
                }
                return instance;
            }
        }

        [UnscopedRef]
        private ref Matrix4 Head => ref Unsafe.As<LightMatrixArray, Matrix4>(ref this);
        [UnscopedRef]
        private readonly ref Matrix4 HeadReadOnly => ref Unsafe.As<LightMatrixArray, Matrix4>(ref Unsafe.AsRef(in this));

        public Span<Matrix4> AsSpan() => MemoryMarshal.CreateSpan(ref Head, (int)CascadeCountConst);

        public readonly ReadOnlySpan<Matrix4> AsReadOnlySpan() => MemoryMarshal.CreateReadOnlySpan(ref HeadReadOnly, (int)CascadeCountConst);
    }

    [StructLayout(LayoutKind.Explicit, Size = sizeof(float) * (int)CascadeCountConst)]
    private unsafe partial struct CascadeFarArray
    {
        const int N = (int)CascadeCountConst * 2;

        [FieldOffset(0)]
        private fixed float _array[N];

        [UnscopedRef]
        private ref float Head => ref Unsafe.As<CascadeFarArray, float>(ref this);
        [UnscopedRef]
        private readonly ref float HeadReadOnly => ref Unsafe.As<CascadeFarArray, float>(ref Unsafe.AsRef(in this));

        public Span<float> AsSpan() => MemoryMarshal.CreateSpan(ref Head, N);

        public readonly ReadOnlySpan<float> AsReadOnlySpan() => MemoryMarshal.CreateReadOnlySpan(ref HeadReadOnly, N);
    }
}
