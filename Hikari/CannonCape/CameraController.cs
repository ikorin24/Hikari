using Hikari;
using Hikari.Mathematics;
using System.Diagnostics;

namespace CannonCape;

public sealed class CameraController
{
    private readonly Cannon _cannon;
    private readonly Camera _camera;
    private static readonly float _yawSpeed = 0.3f.ToRadian();
    private static readonly float _pitchSpeed = 0.2f.ToRadian();
    private bool _isStarted;

    public CameraController(Cannon cannon)
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
        App.Screen.Update.Subscribe(screen =>
        {
            var cannon = _cannon;
            var input = App.Input;
            var changed = false;
            if(input.IsLeftPressed()) {
                cannon.RotateYaw(_yawSpeed);
                changed = true;
            }
            if(input.IsRightPressed()) {
                cannon.RotateYaw(-_yawSpeed);
                changed = true;
            }
            if(input.IsUpPressed()) {
                cannon.RotatePitch(-_pitchSpeed);
                changed = true;
            }
            if(input.IsDownPressed()) {
                cannon.RotatePitch(_pitchSpeed);
                changed = true;
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
        var vec = cannonBackward * 6f + new Vector3(0, 2f, 0);
        _camera.LookAt(cannonObj.Position + new Vector3(0, 1.4f, 0), vec);
    }
}
