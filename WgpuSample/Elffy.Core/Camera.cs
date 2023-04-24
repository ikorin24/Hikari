#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy;

public sealed class Camera
{
    [StructLayout(LayoutKind.Explicit)]
    private struct UniformValue
    {
        [FieldOffset(0)]    // 0 ~ 63   (size: 64)
        public Matrix4 Projection;
        [FieldOffset(64)]   // 64 ~ 127 (size: 64)
        public Matrix4 View;
    }

    private static Vector3 InitialPos => new Vector3(0, 0, 10);
    private static Vector3 InitialDirection => new Vector3(0, 0, -1);
    private static Vector3 InitialUp => Vector3.UnitY;

    private readonly Screen _screen;
    private readonly Own<Buffer> _uniformBuffer;
    private readonly Own<BindGroupLayout> _bindGroupLayout;
    private readonly Own<BindGroup> _bindGroup;
    private readonly object _sync = new object();
    private Matrix4 _view;
    private Matrix4 _projection;
    private Vector3 _position;
    private Quaternion _rotation;
    private Frustum _frustum;
    private float _aspect;  // Aspect ratio (width / height). It may be NaN when height is 0.
    private float _near;
    private float _far;
    private CameraProjectionMode _mode;
    private bool _isUniformChanged;

    private EventSource<Camera> _matrixChanged;

    public Event<Camera> MatrixChanged => _matrixChanged.Event;

    public BindGroupLayout CameraDataBindGroupLayout => _bindGroupLayout.AsValue();
    public BindGroup CameraDataBindGroup => _bindGroup.AsValue();

    public CameraProjectionMode ProjectionMode
    {
        get
        {
            lock(_sync) {
                return _mode;
            }
        }
        set
        {
            if(value.IsInvalid) {
                throw new ArgumentException(nameof(value));
            }
            lock(_sync) {
                _mode = value;
                _projection = CalcProjection(_mode, _near, _far, _aspect);
                Frustum.FromMatrix(_projection, _view, out _frustum);
                _isUniformChanged = true;
            }
            _matrixChanged.Invoke(this);
        }
    }

    public Vector3 Position
    {
        get
        {
            lock(_sync) {
                return _position;
            }
        }
        set
        {
            if(value.ContainsNaNOrInfinity) {
                throw new ArgumentException($"value contains NaN or Infinity");
            }
            lock(_sync) {
                _position = value;
                _view = CalcView(_position, _rotation);
                Frustum.FromMatrix(_projection, _view, out _frustum);
                _isUniformChanged = true;
            }
            _matrixChanged.Invoke(this);
        }
    }

    public Vector3 Direction
    {
        get
        {
            lock(_sync) {
                return _rotation * InitialDirection;
            }
        }
    }

    public Vector3 Up
    {
        get
        {
            lock(_sync) {
                return _rotation * InitialUp;
            }
        }
    }

    public float Far
    {
        get
        {
            lock(_sync) {
                return _far;
            }
        }
    }

    public float Near
    {
        get
        {
            lock(_sync) {
                return _near;
            }
        }
    }

    public Matrix4 GetView()
    {
        lock(_sync) {
            return _view;
        }
    }

    public Matrix4 GetProjection()
    {
        lock(_sync) {
            return _projection;
        }
    }

    internal Camera(Screen screen)
    {
        _screen = screen;
        _view = Matrix4.Identity;
        _projection = Matrix4.Identity;
        _position = InitialPos;
        _rotation = Quaternion.Identity;
        _near = 0.5f;
        _far = 1000f;
        _aspect = 1f;
        _mode = CameraProjectionMode.Perspective(25f / 180f * float.Pi);
        _projection = CalcProjection(_mode, _near, _far, _aspect);
        _view = CalcView(_position, _rotation);
        Frustum.FromMatrix(_projection, _view, out _frustum);

        _uniformBuffer = Buffer.Create(
            screen,
            new UniformValue
            {
                Projection = _projection,
                View = _view,
            },
            BufferUsages.Uniform | BufferUsages.CopyDst);
        _bindGroupLayout = BindGroupLayout.Create(screen, new BindGroupLayoutDescriptor
        {
            Entries = new[]
            {
                BindGroupLayoutEntry.Buffer(
                    0,
                    ShaderStages.Vertex | ShaderStages.Fragment | ShaderStages.Compute,
                    new BufferBindingData
                    {
                        Type = BufferBindingType.Uniform,
                        HasDynamicOffset = false,
                        MinBindingSize = null,
                    }),
            },
        });
        _bindGroup = BindGroup.Create(screen, new BindGroupDescriptor
        {
            Layout = _bindGroupLayout.AsValue(),
            Entries = new[]
            {
                BindGroupEntry.Buffer(0, _uniformBuffer.AsValue()),
            },
        });
    }

    internal void DisposeInternal()
    {
        _bindGroup.Dispose();
        _bindGroupLayout.Dispose();
        _uniformBuffer.Dispose();
    }

    public void SetNearFar(float near, float far)
    {
        if(near <= 0) { ThrowOutOfRange("The value of near is 0 or negative."); }
        if(far <= 0) { ThrowOutOfRange("The value of far is 0 or negative."); }
        if(near > far) { ThrowOutOfRange("The value of near must be smaller than the value of far."); }
        lock(_sync) {
            _near = near;
            _far = far;
            _projection = CalcProjection(_mode, _near, _far, _aspect);
            Frustum.FromMatrix(_projection, _view, out _frustum);
            _isUniformChanged = true;
        }
        _matrixChanged.Invoke(this);
    }

    public void LookAt(in Vector3 target)
    {
        lock(_sync) {
            var dir = (target - _position).Normalized();
            if(dir.ContainsNaNOrInfinity) {
                return;
            }
            _rotation = Quaternion.FromTwoVectors(InitialDirection, dir);
            _view = CalcView(_position, _rotation);
            Frustum.FromMatrix(_projection, _view, out _frustum);
            _isUniformChanged = true;
        }

        _matrixChanged.Invoke(this);
    }

    public void LookAt(in Vector3 target, in Vector3 cameraPos)
    {
        var dir = (target - cameraPos).Normalized();
        if(dir.ContainsNaNOrInfinity) {
            return;
        }
        var rotation = Quaternion.FromTwoVectors(InitialDirection, dir);
        lock(_sync) {
            _rotation = rotation;
            _position = cameraPos;
            _view = CalcView(_position, _rotation);
            Frustum.FromMatrix(_projection, _view, out _frustum);
            _isUniformChanged = true;
        }
        _matrixChanged.Invoke(this);
    }

    internal void ChangeScreenSize(Vector2u size)
    {
        var aspect = (float)size.X / size.Y;
        if(aspect <= 0 || float.IsNaN(aspect)) {
            throw new ArgumentException($"width / height is 0 or NaN. width: {size.X}, height: {size.Y}");
        }
        lock(_sync) {
            _aspect = aspect;
            _projection = CalcProjection(_mode, _near, _far, _aspect);
            Frustum.FromMatrix(_projection, _view, out _frustum);
            _isUniformChanged = true;
        }
        _matrixChanged.Invoke(this);
    }

    internal void UpdateUniformBuffer()
    {
        var uniformBuffer = _uniformBuffer.AsValue();
        lock(_sync) {
            if(_isUniformChanged) {
                _isUniformChanged = false;
                var value = new UniformValue
                {
                    Projection = _projection,
                    View = _view,
                };
                uniformBuffer.Write(0, value);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix4 CalcView(in Vector3 pos, in Quaternion rotation)
    {
        var dir = rotation * InitialDirection;
        var up = rotation * InitialUp;
        return Matrix4.LookAt(pos, pos + dir, up);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix4 CalcProjection(in CameraProjectionMode mode, float near, float far, float aspect)
    {
        if(aspect <= 0) {
            throw new ArgumentException($"{nameof(aspect)} is 0");
        }
        if(float.IsNaN(aspect)) {
            throw new ArgumentException($"{nameof(aspect)} is NaN");
        }

        if(mode.IsPerspective(out var fovy)) {
            Matrix4.PerspectiveProjection(fovy, aspect, near, far, out var projection);
            return projection;
        }
        else if(mode.IsOrthographic(out var height)) {
            var y = height / 2f;
            var x = y * aspect;
            Matrix4.OrthographicProjection(-x, x, -y, y, near, far, out var projection);
            return projection;
        }
        else {
            throw new UnreachableException("invalid camera projection mode");
        }
    }

    [DoesNotReturn]
    private static void ThrowOutOfRange(string message) => throw new ArgumentOutOfRangeException(message);
}

public readonly struct CameraProjectionMode : IEquatable<CameraProjectionMode>
{
    private readonly Mode _mode;
    private readonly float _fovyOrHeight;

    public static CameraProjectionMode Perspective(float fovy)
    {
        if(fovy <= 0 || fovy > MathF.PI) { ThrowOutOfRange($"{nameof(fovy)} must be 0 ~ π. (not include 0)"); }
        return new CameraProjectionMode(Mode.Perspective, fovy);
    }

    public static CameraProjectionMode Orthographic(float height)
    {
        return new CameraProjectionMode(Mode.Orthographic, height);
    }

    private CameraProjectionMode(Mode mode, float fovyOrHeight)
    {
        _mode = mode;
        _fovyOrHeight = fovyOrHeight;
    }

    public bool IsInvalid => _mode == Mode.Invalid;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsPerspective(out float fovy)
    {
        if(_mode == Mode.Perspective) {
            fovy = _fovyOrHeight;
            return true;
        }
        fovy = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsOrthographic(out float height)
    {
        if(_mode == Mode.Orthographic) {
            height = _fovyOrHeight;
            return true;
        }
        height = default;
        return false;
    }

    [DoesNotReturn]
    private static void ThrowOutOfRange(string message) => throw new ArgumentOutOfRangeException(message);

    public override bool Equals(object? obj)
    {
        return obj is CameraProjectionMode mode && Equals(mode);
    }

    public bool Equals(CameraProjectionMode other)
    {
        return _mode == other._mode &&
               _fovyOrHeight == other._fovyOrHeight;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_mode, _fovyOrHeight);
    }

    public static bool operator ==(CameraProjectionMode left, CameraProjectionMode right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CameraProjectionMode left, CameraProjectionMode right)
    {
        return !(left == right);
    }

    private enum Mode
    {
        Invalid = 0,
        Perspective = 1,
        Orthographic = 2,
    }
}
