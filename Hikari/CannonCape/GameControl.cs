using Hikari;
using Hikari.Mathematics;
using System.Diagnostics;

namespace CannonCape;

public sealed class GameControl
{
    private readonly Cannon _cannon;
    private readonly Camera _camera;
    private static readonly float _yawSpeed = 0.1f.ToRadian();
    private static readonly float _pitchSpeed = 0.1f.ToRadian();
    private bool _isStarted;

    public GameControl(Cannon cannon)
    {
        _cannon = cannon;
        var camera = App.Screen.Camera;
        _camera = camera;
    }

    public void Start()
    {
        if(_isStarted) { return; }
        _isStarted = true;
        AdjustCamera();
        _cannon.Obj.Update.Subscribe(screen =>
        {
            var cannon = _cannon;
            var input = App.Input;
            var changed = false;
            if(input.IsArrowLeftPressed()) {
                cannon.RotateYaw(_yawSpeed);
                changed = true;
            }
            if(input.IsArrowRightPressed()) {
                cannon.RotateYaw(-_yawSpeed);
                changed = true;
            }
            if(input.IsArrowUpPressed()) {
                cannon.RotatePitch(-_pitchSpeed);
                changed = true;
            }
            if(input.IsArrowDownPressed()) {
                cannon.RotatePitch(_pitchSpeed);
                changed = true;
            }
            if(input.IsOkDown()) {
                cannon.Fire();
            }
            AdjustCamera();
            if(changed) {
                Debug.WriteLine($"pitch: {cannon.CurrentPitch.ToDegree()}");
            }
        });
    }

    private void AdjustCamera()
    {
        var cannonObj = _cannon.Obj;
        var cannonBackward = cannonObj.Rotation * Vector3.UnitZ;
        var camPos = cannonObj.Position + cannonBackward * 7f + new Vector3(0, 3f, 0);
        var target = cannonObj.Position + new Vector3(0, 1.9f, 0);
        _camera.LookAt(target, camPos);
    }
}
