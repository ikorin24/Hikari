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

    public Boat NewBoat(Vector3 pos, bool useSpawnAnimation)
    {
        return new Boat(_sourceObj, pos, useSpawnAnimation);
    }
}

public sealed class Boat
{
    private static readonly TimeSpan SpawnAnimationTime = TimeSpan.FromSeconds(0.7);
    private static readonly TimeSpan DestroyAnimationTime = TimeSpan.FromSeconds(1.2);
    private readonly FrameObject _obj;

    public event Action<Boat>? OnDestroy;

    public Boat(FrameObject sourceObj, Vector3 pos, bool useSpawnAnimation)
    {
        var obj = sourceObj.Clone();
        obj.IsVisible = true;
        obj.Position = pos;
        _obj = obj;
        Debug.WriteLine($"Boat: {pos}");

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
        _obj.Update.Subscribe(obj =>
        {
            var ratio = (float)(elapsed.TotalSeconds / DestroyAnimationTime.TotalSeconds);
            var angle = 60.ToRadian() * float.Pow(ratio, 3f);
            obj.Rotation = Quaternion.FromAxisAngle(Vector3.UnitZ, angle);

            elapsed += obj.Screen.DeltaTime;
            if(elapsed >= DestroyAnimationTime) {
                obj.Terminate();
            }
        });
    }
}
