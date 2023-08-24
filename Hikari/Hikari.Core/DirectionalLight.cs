#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikari;

public sealed class DirectionalLight : IScreenManaged
{
    private const uint CascadeCountConst = 4;       // 2 ~

    private readonly Screen _screen;
    private readonly Own<Buffer> _buffer;       // DirectionalLightData
    private DirectionalLightData _data;
    private readonly Own<Texture> _shadowMap;
    private readonly Own<Buffer> _lightMatricesBuffer;   // Matrix4[CascadeCount]
    private readonly Own<Buffer> _cascadeFarsBuffer;     // float[CascadeCount]
    private readonly SubscriptionBag _subscriptionBag = new();
    private readonly object _sync = new();

    public Screen Screen => _screen;

    public bool IsManaged => _buffer.IsNone == false;

    public void Validate() => IScreenManaged.DefaultValidate(this);

    public Texture ShadowMap => _shadowMap.AsValue();

    public BufferSlice LightMatricesBuffer => _lightMatricesBuffer.AsValue().Slice();
    public BufferSlice CascadeFarsBuffer => _cascadeFarsBuffer.AsValue().Slice();

    public uint CascadeCount => CascadeCountConst;

    public Vector3 Direction
    {
        get
        {
            lock(_sync) {
                return _data.Direction;
            }
        }
    }

    public Color3 Color
    {
        get
        {
            lock(_sync) {
                return _data.Color;
            }
        }
    }

    public BufferSlice DataBuffer => _buffer.AsValue().Slice();

    public void SetLightData(in Vector3 direction, in Color3 color)
    {
        direction.Normalize();
        if(direction.ContainsNaNOrInfinity) {
            throw new ArgumentException(nameof(direction));
        }
        var buffer = _buffer.AsValue();
        lock(_sync) {
            _data = new DirectionalLightData(direction, color);
            buffer.WriteData(0, _data);
        }
    }

    internal DirectionalLight(Screen screen)
    {
        var data = new DirectionalLightData(
            new Vector3(0f, -1f, -0.3f).Normalized(),
            new Color3(1, 1, 1)
        );
        _screen = screen;
        _buffer = Buffer.CreateInitData(screen, in data, BufferUsages.Storage | BufferUsages.Uniform | BufferUsages.CopyDst);
        _data = data;
        _shadowMap = CreateShadowMap(screen);

        _lightMatricesBuffer = Buffer.Create(screen, (usize)CascadeCountConst * (usize)Matrix4.SizeInBytes, BufferUsages.Storage | BufferUsages.Uniform | BufferUsages.CopyDst);
        _cascadeFarsBuffer = Buffer.Create(screen, (usize)CascadeCountConst * sizeof(float), BufferUsages.Storage | BufferUsages.Uniform | BufferUsages.CopyDst);

        screen.Camera.MatrixChanged.Subscribe(_ => UpdateLightMatrix()).AddTo(_subscriptionBag);
        UpdateLightMatrix();
    }

    private static Own<Texture> CreateShadowMap(Screen screen)
    {
        const u32 Width = 1024 * CascadeCountConst;
        const u32 Height = 1024;
        return Texture.Create(screen, new()
        {
            Dimension = TextureDimension.D2,
            Size = new Vector3u(Width, Height, 1u),
            Format = TextureFormat.Depth32Float,
            MipLevelCount = 1,
            SampleCount = 1,
            Usage = TextureUsages.TextureBinding | TextureUsages.RenderAttachment | TextureUsages.CopySrc,
        });
    }

    internal void DisposeInternal()
    {
        _buffer.Dispose();
        _shadowMap.Dispose();
        _lightMatricesBuffer.Dispose();
        _cascadeFarsBuffer.Dispose();
        _subscriptionBag.Dispose();
    }

    [SkipLocalsInit]
    internal void UpdateLightMatrix()
    {
        var camera = _screen.Camera;
        var lightDirection = Direction;
        Span<Matrix4> lightMatrices = stackalloc Matrix4[(int)CascadeCountConst];
        Span<float> cascadeFars = stackalloc float[(int)CascadeCountConst];
        CalcLightMatrix(lightDirection, camera.ReadState(), lightMatrices, cascadeFars);

        var lightMatricesBuffer = _lightMatricesBuffer.AsValue();
        var cascadeFarsBuffer = _cascadeFarsBuffer.AsValue();
        lightMatricesBuffer.WriteSpan(0, lightMatrices.AsReadOnly());
        cascadeFarsBuffer.WriteSpan(0, cascadeFars.AsReadOnly());
    }

    private static void CalcLightMatrix(
        in Vector3 lightDir,
        in Camera.CameraState camera,
        Span<Matrix4> lightMatrices,
        Span<float> cascadeFars)
    {
        Vector3 lUp;
        {
            var dirX0Z = new Vector3(lightDir.X, 0, lightDir.Z).Normalized();
            lUp = dirX0Z.ContainsNaNOrInfinity ?
                Vector3.UnitX :
                Quaternion.FromTwoVectors(dirX0Z, lightDir) * Vector3.UnitY;
        }

        const float MaxShadowMapFar = 500;

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

            Matrix4 cascadedProj;
            if(camera.ProjectionMode.IsPerspective(out var fovy)) {
                cascadedProj = Matrix4.PerspectiveProjection(fovy, camera.Aspect, n, f);
            }
            else if(camera.ProjectionMode.IsOrthographic(out var height)) {
                throw new NotImplementedException();        // TODO:
            }
            else {
                throw new NotSupportedException();
            }
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
            var lproj = Matrix4.OrthographicProjection(
                aabbInLightSpace.Min.X, aabbInLightSpace.Max.X,
                aabbInLightSpace.Min.Y, aabbInLightSpace.Max.Y,
                -(aabbInLightSpace.Max.Z + float.Clamp(aabbInLightSpace.Size.Z * 2.0f, 10, 200)),  // TODO:
                -aabbInLightSpace.Min.Z);
            lightMatrices[i] = lproj * lview;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    private readonly struct DirectionalLightData
    {
        private readonly Vector4 _direction;    // (x, y, z, _)
        private readonly Color4 _color;        // (r, g, b, _)

        public Vector3 Direction => _direction.Xyz;
        public Color3 Color => _color.ToColor3();

        public DirectionalLightData(in Vector3 direction, in Color3 color)
        {
            _direction = new Vector4(direction, 0);
            _color = new Color4(color, 0);
        }
    }
}
