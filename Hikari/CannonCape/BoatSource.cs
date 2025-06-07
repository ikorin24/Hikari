using Cysharp.Threading.Tasks;
using Hikari;
using Hikari.Mathematics;
using System;
using System.Diagnostics;

namespace CannonCape;

public sealed class BoatSource
{
    private readonly FrameObject _sourceObj;

    private BoatSource(FrameObject sourceObj)
    {
        _sourceObj = sourceObj;
    }

    public static async UniTask<BoatSource> Create()
    {
        var sourceObj = await GlbLoadHelper.LoadResource("boat.glb", false);
        return new BoatSource(sourceObj);
    }

    public Boat NewBoat(Vector3 pos, Vector3 dest, bool useSpawnAnimation)
    {
        return new Boat(_sourceObj, pos, dest, useSpawnAnimation);
    }
}

public sealed class Boat
{
    private static readonly TimeSpan SpawnAnimationTime = TimeSpan.FromSeconds(0.7);
    private static readonly TimeSpan DestroyAnimationTime = TimeSpan.FromSeconds(1.2);
    private readonly FrameObject _obj;
    private readonly Vector3 _dest;

    public event Action<Boat>? OnDestroy;

    public Boat(FrameObject sourceObj, Vector3 pos, Vector3 dest, bool useSpawnAnimation)
    {
        _dest = dest;
        var obj = sourceObj.Clone();
        obj.IsVisible = true;
        obj.Position = pos;
        obj.Rotation = Quaternion.FromTwoVectors(-Vector3.UnitZ, (pos - dest).Normalized());
        _obj = obj;
        Debug.WriteLine($"Boat: {pos}");

        obj.Update.Subscribe(_ => Update());

        if(useSpawnAnimation) {
            var scale = obj.Scale;
            obj.Scale = new Vector3(0);
            var elapsed = TimeSpan.Zero;

            var subscription = EventSubscription<FrameObject>.None;
            subscription = obj.Update.Subscribe(obj =>
            {
                var ratio = (float)(elapsed.TotalSeconds / SpawnAnimationTime.TotalSeconds);
                obj.Scale = scale * ratio;
                elapsed += obj.Screen.DeltaTime;
                if(elapsed >= SpawnAnimationTime) {
                    obj.Scale = scale;
                    subscription.Dispose();
                }
            });
        }
    }

    public (Vector3 Center, float Radius) SphereCollider =>
        (
            Center: _obj.Position + new Vector3(0, 0.6f, 0),
            Radius: 1.7f
        );

    public void Destroy()
    {
        OnDestroy?.Invoke(this);

        var elapsed = TimeSpan.Zero;
        var currentRot = _obj.Rotation;
        _obj.Update.Subscribe(obj =>
        {
            var ratio = (float)(elapsed.TotalSeconds / DestroyAnimationTime.TotalSeconds);
            var angle = 60.ToRadian() * float.Pow(ratio, 3f);
            obj.Rotation = currentRot * Quaternion.FromAxisAngle(Vector3.UnitZ, angle);

            elapsed += obj.Screen.DeltaTime;
            if(elapsed >= DestroyAnimationTime) {
                obj.Terminate();
            }
        });
    }

    private void Update()
    {
        var diff = _dest - _obj.Position;
        if(diff.Z > 0) {
            var vec = diff.Normalized();
            const float Velocity = 1.5f;
            _obj.Position += vec * Velocity * App.Screen.DeltaTimeSec;
        }
    }
}
