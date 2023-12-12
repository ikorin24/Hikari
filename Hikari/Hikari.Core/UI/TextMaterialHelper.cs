#nullable enable
using Hikari.Mathematics;
using System.Diagnostics;

namespace Hikari.UI;

internal static class TextMaterialHelper
{
    public static (MaybeOwn<Texture2D> NewTexture, Vector2u ContentSize, bool Changed) UpdateTextTexture(Screen screen, Texture2D? texture, string text, FontSize fontSize, ColorByte color, float scaleFactor)
    {
        using var font = new SkiaSharp.SKFont();
        font.Size = fontSize.Px * scaleFactor;
        var options = new TextDrawOptions
        {
            Background = ColorByte.Transparent,
            Foreground = color,
            PowerOfTwoSizeRequired = true,
            Font = font,
        };
        var arg = (screen, texture);
        return TextDrawer.Draw(text, options, arg, static result =>
        {
            var (screen, t) = result.Arg;
            var image = result.Image;
            if(image.Size.X == 0) {
                Debug.Assert(image.Size.Y == 0);
                var emptyTexture = UIShader.GetEmptyTexture2D(screen);
                return (emptyTexture, Vector2u.One, true);
            }
            Debug.Assert(MathTool.IsPowerOfTwo(image.Size.X));
            Debug.Assert(MathTool.IsPowerOfTwo(image.Size.Y));
            if(t is Texture2D texture && texture.Usage.HasFlag(TextureUsages.CopyDst) && texture.Size == (Vector2u)image.Size) {
                Debug.Assert(texture.Format == TextureFormat.Rgba8UnormSrgb);
                Debug.Assert(texture.MipLevelCount == 1);
                texture.Write(0, image.GetPixels());
                return (texture, result.TextBoundsSize, false);
            }
            else {
                var newTexture = Texture2D.CreateFromRawData(screen, new()
                {
                    Format = TextureFormat.Rgba8UnormSrgb,
                    MipLevelCount = 1,
                    SampleCount = 1,
                    Size = (Vector2u)image.Size,
                    Usage = TextureUsages.TextureBinding | TextureUsages.CopyDst,
                }, image.GetPixels().AsBytes());
                return (MaybeOwn.New(newTexture), result.TextBoundsSize, true);
            }
        });
    }
}
