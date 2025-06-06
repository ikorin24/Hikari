using Cysharp.Threading.Tasks;
using Hikari;

namespace CannonCape;

public sealed class Ground
{
    private readonly FrameObject _obj;

    private Ground(FrameObject obj)
    {
        _obj = obj;
    }

    public static async UniTask<Ground> Load()
    {
        var ground = await GlbLoadHelper.LoadResource("ground.glb");
        return new Ground(ground);
    }
}
