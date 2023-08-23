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
    internal struct CameraMatrix
    {
        [FieldOffset(0)]    // 0 ~ 63   (size: 64)
        public Matrix4 Projection;
        [FieldOffset(64)]   // 64 ~ 127 (size: 64)
        public Matrix4 View;
        [FieldOffset(128)]  // 128 ~ 191 (size: 64)
        public Matrix4 InvProjection;
        [FieldOffset(192)]  // 192 ~ 255 (size: 64)
        public Matrix4 InvView;
    }

    internal struct CameraState
    {
        public CameraMatrix Matrix;
        public Vector3 Position;
        public Quaternion Rotation;
        public CameraProjectionMode ProjectionMode;
        public float Aspect;    // Aspect ratio (width / height).
        public float Near;
        public float Far;

        public Vector3 GetDirection() => Rotation * InitialDirection;
        public Vector3 GetUp() => Rotation * InitialUp;
    }

    private static Vector3 InitialPos => new Vector3(0, 0, 10);
    private static Vector3 InitialDirection => new Vector3(0, 0, -1);
    private static Vector3 InitialUp => Vector3.UnitY;

    private static Matrix4 GLToWebGpu => new Matrix4(
        new Vector4(1, 0, 0, 0),
        new Vector4(0, 1, 0, 0),
        new Vector4(0, 0, 0.5f, 0),
        new Vector4(0, 0, 0.5f, 1));

    private readonly Screen _screen;
    private readonly Own<Buffer> _cameraMatrixBuffer;
    private readonly Own<BindGroupLayout> _bindGroupLayout;
    private readonly Own<BindGroup> _bindGroup;
    private readonly object _sync = new object();
    private CameraState _state;
    private bool _isCameraMatrixChanged;
    private EventSource<Camera> _matrixChanged;

    public Event<Camera> MatrixChanged => _matrixChanged.Event;

    public Screen Screen => _screen;

    public BindGroupLayout CameraDataBindGroupLayout => _bindGroupLayout.AsValue();
    public BindGroup CameraDataBindGroup => _bindGroup.AsValue();

    public CameraProjectionMode ProjectionMode
    {
        get
        {
            lock(_sync) {
                return _state.ProjectionMode;
            }
        }
        set
        {
            if(value.IsInvalid) {
                throw new ArgumentException(nameof(value));
            }
            lock(_sync) {
                _state.ProjectionMode = value;
                _state.Matrix.Projection = CalcProjection(_state.ProjectionMode, _state.Near, _state.Far, _state.Aspect);
                _state.Matrix.InvProjection = _state.Matrix.Projection.Inverted();
                _isCameraMatrixChanged = true;
            }
            _matrixChanged.Invoke(this);
        }
    }

    public Vector3 Position
    {
        get
        {
            lock(_sync) {
                return _state.Position;
            }
        }
        set
        {
            if(value.ContainsNaNOrInfinity) {
                throw new ArgumentException($"value contains NaN or Infinity");
            }
            lock(_sync) {
                var valid = CalcView(_state.Position, _state.Rotation, out var view);
                Debug.Assert(valid);
                _state.Position = value;
                _state.Matrix.View = view;
                _state.Matrix.InvView = view.Inverted();
                _isCameraMatrixChanged = true;
            }
            _matrixChanged.Invoke(this);
        }
    }

    public Quaternion Rotation
    {
        get
        {
            lock(_sync) {
                return _state.Rotation;
            }
        }
    }

    public Vector3 Direction
    {
        get
        {
            lock(_sync) {
                return _state.GetDirection();
            }
        }
    }

    public Vector3 Up
    {
        get
        {
            lock(_sync) {
                return _state.GetUp();
            }
        }
    }

    public float Far
    {
        get
        {
            lock(_sync) {
                return _state.Far;
            }
        }
    }

    public float Near
    {
        get
        {
            lock(_sync) {
                return _state.Near;
            }
        }
    }

    public float Aspect
    {
        get
        {
            lock(_sync) {
                return _state.Aspect;
            }
        }
    }

    public Matrix4 GetView()
    {
        lock(_sync) {
            return _state.Matrix.View;
        }
    }

    public Matrix4 GetProjection()
    {
        lock(_sync) {
            return _state.Matrix.Projection;
        }
    }

    internal delegate T ReadStateFunc<T>(in CameraState state);

    internal CameraState ReadState()
    {
        lock(_sync) {
            return _state;
        }
    }

    internal T ReadState<T>(ReadStateFunc<T> func)
    {
        lock(_sync) {
            return func(_state);
        }
    }

    internal Camera(Screen screen)
    {
        _screen = screen;

        var mode = CameraProjectionMode.Perspective(25f / 180f * MathF.PI);
        var aspect = 1f;
        var near = 0.5f;
        var far = 1000f;
        var rot = Quaternion.Identity;
        var projection = CalcProjection(mode, near, far, aspect);
        Matrix4 view;
        {
            var valid = CalcView(InitialPos, rot, out view);
            Debug.Assert(valid);
        }
        _state = new CameraState
        {
            Matrix = new CameraMatrix
            {
                Projection = projection,
                View = view,
                InvProjection = projection.Inverted(),
                InvView = view.Inverted(),
            },
            Position = InitialPos,
            Rotation = rot,
            Aspect = aspect,
            Near = near,
            Far = far,
            ProjectionMode = mode,
        };
        _cameraMatrixBuffer = Buffer.CreateInitData(
            screen,
            _state.Matrix,
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
                BindGroupEntry.Buffer(0, _cameraMatrixBuffer.AsValue()),
            },
        });
    }

    internal void DisposeInternal()
    {
        _bindGroup.Dispose();
        _bindGroupLayout.Dispose();
        _cameraMatrixBuffer.Dispose();
    }

    public void SetNearFar(float near, float far)
    {
        if(near <= 0) { ThrowOutOfRange("The value of near is 0 or negative."); }
        if(far <= 0) { ThrowOutOfRange("The value of far is 0 or negative."); }
        if(near > far) { ThrowOutOfRange("The value of near must be smaller than the value of far."); }
        lock(_sync) {
            _state.Near = near;
            _state.Far = far;
            _state.Matrix.Projection = CalcProjection(_state.ProjectionMode, _state.Near, _state.Far, _state.Aspect);
            _state.Matrix.InvProjection = _state.Matrix.Projection.Inverted();
            _isCameraMatrixChanged = true;
        }
        _matrixChanged.Invoke(this);
    }

    public bool LookAt(in Vector3 target)
    {
        lock(_sync) {
            var pos = _state.Position;
            var dir = (target - pos).Normalized();
            if(dir.ContainsNaNOrInfinity) {
                return false;
            }
            if(CalcView(pos, _state.Rotation, out var view) == false) {
                return false;
            }
            _state.Rotation = Quaternion.FromTwoVectors(InitialDirection, dir);
            _state.Matrix.View = view;
            _state.Matrix.InvView = view.Inverted();
            _isCameraMatrixChanged = true;
        }
        _matrixChanged.Invoke(this);
        return true;
    }

    public bool LookAt(in Vector3 target, in Vector3 cameraPos)
    {
        var dir = (target - cameraPos).Normalized();
        if(dir.ContainsNaNOrInfinity) {
            return false;
        }
        var rotation = Quaternion.FromTwoVectors(InitialDirection, dir);
        if(CalcView(cameraPos, rotation, out var view) == false) {
            return false;
        }

        lock(_sync) {
            _state.Rotation = rotation;
            _state.Position = cameraPos;
            _state.Matrix.View = view;
            _state.Matrix.InvView = view.Inverted();
            _isCameraMatrixChanged = true;
        }
        _matrixChanged.Invoke(this);
        return true;
    }

    public void GetFrustum(out Frustum frustum)
    {
        lock(_sync) {
            Frustum.FromMatrix(_state.Matrix.Projection, _state.Matrix.View, out frustum);
        }
    }

    public Frustum GetFrustum()
    {
        GetFrustum(out var frustum);
        return frustum;
    }

    internal void ChangeScreenSize(Vector2u size)
    {
        var aspect = (float)size.X / size.Y;
        if(aspect <= 0 || float.IsNaN(aspect)) {
            throw new ArgumentException($"width / height is 0 or NaN. width: {size.X}, height: {size.Y}");
        }
        lock(_sync) {
            _state.Aspect = aspect;
            _state.Matrix.Projection = CalcProjection(_state.ProjectionMode, _state.Near, _state.Far, _state.Aspect);
            _state.Matrix.InvProjection = _state.Matrix.Projection.Inverted();
            _isCameraMatrixChanged = true;
        }
        _matrixChanged.Invoke(this);
    }

    internal void UpdateUniformBuffer()
    {
        var uniformBuffer = _cameraMatrixBuffer.AsValue();
        lock(_sync) {
            if(_isCameraMatrixChanged) {
                _isCameraMatrixChanged = false;
                uniformBuffer.WriteData(0, _state.Matrix);
            }
        }
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //private static Matrix4 CalcView(in Vector3 pos, in Quaternion rotation)
    //{
    //    var dir = rotation * InitialDirection;
    //    if(Matrix4.LookAt(pos, pos + dir, Vector3.UnitY, out var result) == false) {
    //        result = Matrix4.Identity;
    //    }
    //    return result;
    //}
    private static bool CalcView(in Vector3 pos, in Quaternion rotation, out Matrix4 view)
    {
        var dir = rotation * InitialDirection;
        return Matrix4.LookAt(pos, pos + dir, Vector3.UnitY, out view);
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
            return GLToWebGpu * Matrix4.PerspectiveProjection(fovy, aspect, near, far);
        }
        else if(mode.IsOrthographic(out var height)) {
            var y = height / 2f;
            var x = y * aspect;
            return GLToWebGpu * Matrix4.OrthographicProjection(-x, x, -y, y, near, far);
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
