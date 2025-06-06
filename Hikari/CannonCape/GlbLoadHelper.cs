using Cysharp.Threading.Tasks;
using Hikari;
using Hikari.Gltf;

namespace CannonCape;

public static class GlbLoadHelper
{
    public static async UniTask<FrameObject> LoadResource(string name)
    {
        var (obj, disposables) = await GlbModelLoader.LoadGlbFileAsync(App.PbrShader, Resources.Path(name));
        disposables.DisposeOn(App.Screen.Closed);
        return obj;
    }
}
