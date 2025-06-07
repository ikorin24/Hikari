using Cysharp.Threading.Tasks;
using Hikari;
using Hikari.Mathematics;
using System;
using System.Diagnostics;

namespace CannonCape;

public sealed class BoatSource
{
    private readonly FrameObject _sourceObj;
    private readonly ShotSource _shotSource;

    private BoatSource(FrameObject sourceObj, ShotSource shotSource)
    {
        _sourceObj = sourceObj;
        _shotSource = shotSource;
    }

    public static async UniTask<BoatSource> Load(ShotSource shotSource)
    {
        var sourceObj = await GlbLoadHelper.LoadResource("boat.glb", false);
        return new BoatSource(sourceObj, shotSource);
    }

    public Boat NewBoat(Vector3 pos, Vector3 dest, bool useSpawnAnimation)
    {
        return new Boat(_sourceObj, pos, dest, _shotSource, useSpawnAnimation);
    }
}

public sealed class Boat : ISphereCollider
{
    private static readonly TimeSpan SpawnAnimationTime = TimeSpan.FromSeconds(0.7);
    private static readonly TimeSpan DestroyAnimationTime = TimeSpan.FromSeconds(1.0);
    private readonly FrameObject _obj;
    private readonly Vector3 _dest;

    public event Action<Boat>? OnDestroy;

    public (Vector3 Center, float Radius) SphereCollider =>
    (
        Center: _obj.Position + new Vector3(0, 0.6f, 0),
        Radius: 1.7f
    );

    public Boat(FrameObject sourceObj, Vector3 pos, Vector3 dest, ShotSource shotSource, bool useSpawnAnimation)
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

        UniTask.Void(async () =>
        {
            var rand = Random.Shared;
            while(obj.LifeState != LifeState.Dead) {
                var delay = rand.NextSingle() * 5 + 10;
                await App.Screen.Update.Delay(TimeSpan.FromSeconds(delay));
                if(obj.LifeState == LifeState.Dead) {
                    return;
                }

                // 敵の距離が遠いと命中半径が大きくなる、近いと命中半径が小さくなる
                const float FarDistance = 250;
                const float NearDistance = 100;
                const float RandomRadiusMax = 7;
                const float RandomRadiusMin = 2.5f;
                var distance = obj.Position.Length;
                var a = float.Clamp(1f - (distance - NearDistance) / (FarDistance - NearDistance), 0f, 1f);
                var randomRadius = a * (RandomRadiusMax - RandomRadiusMin) + RandomRadiusMin;
                var target = -obj.Position + new Vector3(0, 6f, 0);
                target += new Vector3
                {
                    X = rand.NextSingle() * randomRadius * 2 - randomRadius,
                    Y = rand.NextSingle() * randomRadius * 2 - randomRadius,
                };
                var vec = target.Normalized();
                var velocity = vec * 2.5f;
                shotSource.NewEnemyShot(obj.Position, velocity);
            }
        });
    }

    void ISphereCollider.OnColliderHit()
    {
        Destroy();
    }

    private void Destroy()
    {
        OnDestroy?.Invoke(this);

        var elapsed = TimeSpan.Zero;
        var currentRot = _obj.Rotation;
        _obj.Update.Subscribe(obj =>
        {
            var ratio = (float)(elapsed.TotalSeconds / DestroyAnimationTime.TotalSeconds);
            var angle = 70.ToRadian() * float.Pow(ratio, 3f);
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
