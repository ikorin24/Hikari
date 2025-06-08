using Cysharp.Threading.Tasks;
using Hikari;
using System;

namespace CannonCape;

public sealed class Ground : ISphereCollider, IDisposable
{
    private readonly FrameObject _obj;

    public event Action? EnemyShotHit;

    public FrameObject Obj => _obj;

    public (Vector3 Center, float Radius) SphereCollider =>
        (
            Center: new Vector3(0, 6, 0),
            Radius: 1f
        );

    private Ground(FrameObject obj)
    {
        _obj = obj;
    }

    public static async UniTask<Ground> Load()
    {
        var ground = await GlbLoadHelper.LoadResource("ground.glb");
        return new Ground(ground);
    }

    void ISphereCollider.OnColliderHit()
    {
        EnemyShotHit?.Invoke();
    }

    public void Dispose()
    {
        _obj.Terminate();
    }
}
