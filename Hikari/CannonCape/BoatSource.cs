using Cysharp.Threading.Tasks;
using Hikari;
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

    public void NewBoat(Vector3 pos, bool useSpawnAnimation)
    {
        _ = new Boat(_sourceObj, pos, useSpawnAnimation);
    }

    private sealed class Boat
    {
        private static readonly TimeSpan SpawnAnimationTime = TimeSpan.FromSeconds(0.7);

        public Boat(FrameObject sourceObj, Vector3 pos, bool useSpawnAnimation)
        {
            var obj = sourceObj.Clone();
            obj.IsVisible = true;
            obj.Position = pos;
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
    }
}
