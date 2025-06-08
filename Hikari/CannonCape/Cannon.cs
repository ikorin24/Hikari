using Cysharp.Threading.Tasks;
using Hikari;
using Hikari.Mathematics;
using System;
using System.Diagnostics;

namespace CannonCape;

public sealed class Cannon : IDisposable
{
    private readonly FrameObject _cannon;
    private readonly FrameObject _cylinder;
    private readonly ShotSource _shotSource;
    private float _currentPitch;

    private static readonly float _yawMaxAbs = 25f.ToRadian();
    private static readonly float _pitchMax = 60f.ToRadian();

    public FrameObject Obj => _cannon;

    public float CurrentPitch => _currentPitch;

    public event Action<float>? PitchChanged;

    private Cannon(FrameObject obj, FrameObject cylinder, ShotSource shotSource)
    {
        _cannon = obj;
        _cylinder = cylinder;
        _shotSource = shotSource;
        _currentPitch = Vector3.AngleBetween(-Vector3.UnitZ, cylinder.Rotation * -Vector3.UnitZ);
    }

    public void Fire()
    {
        Debug.Assert(_cannon.Screen.MainThread.IsCurrentThread);
        var shotPos = _cannon.Position + new Vector3(0, 0.45f * 1.4f, 0);
        var vec = _cannon.Rotation * Quaternion.FromAxisAngle(Vector3.UnitX, _currentPitch) * -Vector3.UnitZ;
        var velocity = vec * 3f;
        _shotSource.NewShot(shotPos, velocity);
        Debug.WriteLine($"Fire: {velocity}, pitch: {_currentPitch.ToDegree()}");
    }

    public static async UniTask<Cannon> Load(ShotSource shotSource)
    {
        var (cylinder, cannonBase) = await UniTask.WhenAll(
            GlbLoadHelper.LoadResource("cannon_cylinder.glb"),
            GlbLoadHelper.LoadResource("cannon_base.glb"));
        cylinder.Position = new Vector3(0, 0.45f, 0);
        cylinder.Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, 30.ToRadian());
        var cannon = new FrameObject(App.Screen)
        {
            Scale = new Vector3(1.4f),
            Children =
            [
                cylinder,
                cannonBase,
            ],
        };
        return new Cannon(cannon, cylinder, shotSource);
    }

    public void RotateYaw(float angleDiff)
    {
        var rot = Quaternion.FromAxisAngle(Vector3.UnitY, angleDiff) * _cannon.Rotation;
        var angleBetween = Vector3.AngleBetween(-Vector3.UnitZ, rot * -Vector3.UnitZ);
        if(angleBetween > _yawMaxAbs) {
            rot = Quaternion.FromAxisAngle(Vector3.UnitY, float.Sign(angleDiff) * _yawMaxAbs);
        }
        _cannon.Rotation = rot;
    }

    public void RotatePitch(float angleDiff)
    {
        var rot = Quaternion.FromAxisAngle(Vector3.UnitX, angleDiff) * _cylinder.Rotation;
        if((rot * -Vector3.UnitZ).Y < 0) {
            rot = Quaternion.FromAxisAngle(-Vector3.UnitZ, 0);
        }
        else {
            var angleBetween = Vector3.AngleBetween(-Vector3.UnitZ, rot * -Vector3.UnitZ);
            if(angleBetween > _pitchMax) {
                rot = Quaternion.FromAxisAngle(Vector3.UnitX, _pitchMax);
            }
        }
        _cylinder.Rotation = rot;
        _currentPitch = Vector3.AngleBetween(-Vector3.UnitZ, rot * -Vector3.UnitZ);
        PitchChanged?.Invoke(_currentPitch);
    }

    public void Dispose()
    {
        _cannon.Terminate();
        _cylinder.Terminate();
        _shotSource.Dispose();
    }
}
