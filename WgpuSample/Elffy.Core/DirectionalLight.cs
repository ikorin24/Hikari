#nullable enable
using Elffy.Effective;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy;

public sealed class DirectionalLight : IScreenManaged
{
    private readonly Screen _screen;
    private readonly Own<Buffer> _buffer;       // DirectionalLightData
    private DirectionalLightData _data;
    private Own<Texture> _shadowMap;
    private Own<Buffer> _lightMatricesBuffer;   // Matrix4[CascadeCount]

    private const int CascadeCountConst = 4;

    private readonly object _sync = new();

    public Screen Screen => _screen;

    public bool IsManaged => _buffer.IsNone == false;

    public void Validate() => IScreenManaged.DefaultValidate(this);

    public Texture ShadowMap => _shadowMap.AsValue();

    public BufferSlice<byte> LightMatricesBuffer => _lightMatricesBuffer.AsValue().Slice();

    public int CascadeCount => CascadeCountConst;

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

    public BufferSlice<byte> DataBuffer => _buffer.AsValue().Slice();

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
    }

    private static Own<Texture> CreateShadowMap(Screen screen)
    {
        const u32 Width = 1024;
        const u32 Height = 1024;
        return Texture.Create(screen, new()
        {
            Dimension = TextureDimension.D2,
            Size = new Vector3u(Width, Height, 1u),
            Format = TextureFormat.Depth32Float,
            MipLevelCount = 1,
            SampleCount = 1,
            Usage = TextureUsages.TextureBinding | TextureUsages.RenderAttachment,
        });
    }

    internal void DisposeInternal()
    {
        _buffer.Dispose();
        _shadowMap.Dispose();
        _lightMatricesBuffer.Dispose();
    }

    [SkipLocalsInit]
    internal void UpdateLightMatrix()
    {
        var camera = _screen.Camera;
        var lightDir = Direction;
        Vector3 lightUp;
        {
            var dirX0Z = new Vector3(lightDir.X, 0, lightDir.Z).Normalized();
            lightUp = dirX0Z.ContainsNaNOrInfinity ?
                Vector3.UnitX :
                Quaternion.FromTwoVectors(dirX0Z, lightDir) * Vector3.UnitY;
        }

        Span<Matrix4> lightMatrices = stackalloc Matrix4[CascadeCountConst];
        CalcLightMatrix(lightDir, lightUp, camera, lightMatrices);
        var buffer = _lightMatricesBuffer.AsValue();
        buffer.WriteSpan(0, lightMatrices.AsReadOnly());
    }

    private static void CalcLightMatrix(
        in Vector3 lightDir, in Vector3 lightUp,
        Camera camera,
        Span<Matrix4> lightMatrices)
    {
        var (far, near, aspect, projMode, view) = camera.ReadState(
            (in Camera.CameraState s) => (s.Far, s.Near, s.Aspect, s.ProjectionMode, s.Matrix.View)
        );

        var nearToFar = far - near;
        for(int i = 0; i < lightMatrices.Length; i++) {
            // TODO: 線形ではない分割方法を考える
            var n = near + nearToFar * (float)i / lightMatrices.Length;        // i 番目のカスケードシャドウの NearPlane の中心位置
            var f = near + nearToFar * (float)(i + 1) / lightMatrices.Length;  // i 番目のカスケードシャドウの FarPlane の中心位置
            Matrix4 cascadedProj;
            if(projMode.IsPerspective(out var fovy)) {
                Matrix4.PerspectiveProjection(fovy, aspect, n, f, out cascadedProj);
            }
            else if(projMode.IsOrthographic(out var height)) {
                throw new NotImplementedException();        // TODO:
            }
            else {
                throw new NotSupportedException();
            }

            Frustum.FromMatrix(cascadedProj, view, out var cascadedFrustum);
            var center = cascadedFrustum.Center;
            var lview = Matrix4.LookAt(center - lightDir, center, lightUp);

            var min = Vector3.MaxValue;
            var max = Vector3.MinValue;
            foreach(var corner in cascadedFrustum.Corners) {
                var p = lview.Transform(corner);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }

            var lAabb = Bounds.FromMinMax(min, max);
            var lightFar = -lAabb.Min.Z + float.Clamp(lAabb.Size.Z * 5, 10, 1000);  // TODO: depth ちゃんと決める
            var lightNear = -lAabb.Max.Z - float.Clamp(lAabb.Size.Z * 5, 10, 1000); // TODO: depth ちゃんと決める
            Matrix4.OrthographicProjection(
                lAabb.Min.X, lAabb.Max.X,
                lAabb.Min.Y, lAabb.Max.Y,
                lightNear,
                lightFar,
                out var lproj);
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
