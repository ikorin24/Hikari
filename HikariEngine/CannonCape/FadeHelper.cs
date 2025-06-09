using Cysharp.Threading.Tasks;
using Hikari.UI;
using System;

namespace CannonCape;

public static class FadeHelper
{
    private const float DefaultFadeSeconds = 0.8f;

    public static async UniTask FadeOut(UIElement element, float fadeSeconds = DefaultFadeSeconds)
    {
        var span = TimeSpan.FromSeconds(fadeSeconds);
        var start = App.CurrentTime;
        while(true) {
            var elapsed = App.CurrentTime - start;
            if(elapsed >= span) {
                element.Background = Brush.Black;
                return;
            }
            var alpha = (float)elapsed.Ticks / span.Ticks;
            element.Background = Brush.Solid(0, 0, 0, alpha);
            await App.Screen.Update.Switch();
        }
    }

    public static async UniTask FadeIn(UIElement element, float fadeSeconds = DefaultFadeSeconds)
    {
        var span = TimeSpan.FromSeconds(fadeSeconds);
        var start = App.CurrentTime;
        while(true) {
            var elapsed = App.CurrentTime - start;
            if(elapsed >= span) {
                element.Background = Brush.Transparent;
                return;
            }
            var alpha = (float)elapsed.Ticks / span.Ticks;
            element.Background = Brush.Solid(0, 0, 0, 1f - alpha);
            await App.Screen.Update.Switch();
        }
    }
}
