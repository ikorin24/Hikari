using Cysharp.Threading.Tasks;
using Hikari;
using Hikari.Mathematics;

namespace CannonCape;

public sealed class Cannon
{
    private readonly FrameObject _cannon;
    private readonly FrameObject _cylinder;
    private float _currentPitch;

    private static readonly float _yawMaxAbs = 45f.ToRadian();
    private static readonly float _pitchMax = 60f.ToRadian();

    public FrameObject Obj => _cannon;

    public float CurrentPitch => _currentPitch;

    private Cannon(FrameObject obj, FrameObject cylinder)
    {
        _cannon = obj;
        _cylinder = cylinder;
    }

    public static async UniTask<Cannon> Load()
    {
        var (cylinder, cannonBase) = await UniTask.WhenAll(
            GlbLoadHelper.LoadResource("cannon_cylinder.glb"),
            GlbLoadHelper.LoadResource("cannon_base.glb"));
        cylinder.Position = new Vector3(0, 0.45f, 0);
        cylinder.Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, 30.ToRadian());
        var cannon = new FrameObject(App.Screen)
        {
            Children =
            [
                cylinder,
                cannonBase,
            ],
        };
        return new Cannon(cannon, cylinder);
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
    }
}
