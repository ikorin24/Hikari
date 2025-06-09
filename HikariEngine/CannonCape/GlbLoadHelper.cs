using Cysharp.Threading.Tasks;
using Hikari;
using Hikari.Gltf;

namespace CannonCape;

public static class GlbLoadHelper
{
    public static async UniTask<FrameObject> LoadResource(string name)
    {
        return await LoadResource(name, true);
    }

    public static async UniTask<FrameObject> LoadResource(string name, bool visible)
    {
        var shader = App.PbrShader;
        var path = Resources.Path(name);
        var (obj, disposables) = visible
            ? await GlbModelLoader.LoadGlbFileAsync(shader, path)
            : await GlbModelLoader.LoadGlbFileAsync(shader, path, new FrameObjectInitArg { IsVisible = false });
        disposables.DisposeOn(App.Screen.Closed);
        return obj;
    }
}
