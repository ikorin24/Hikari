#nullable enable
using Hikari.Mathematics;
using System.Diagnostics;

namespace Hikari.UI;

internal static class TextMaterialHelper
{
    public static void UpdateTextTexture(UIMaterial material, string text, FontSize fontSize, ColorByte color, float scaleFactor)
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
        TextDrawer.Draw(text, options, material, static result =>
        {
            var material = result.Arg;
            var image = result.Image;
            if(image.Size.X == 0) {
                Debug.Assert(image.Size.Y == 0);
                if(material.Texture != material.Shader.EmptyTexture) {
                    material.UpdateTexture(material.Shader.EmptyTexture);
                }
                material.UpdateTextureContentSize(Vector2u.One);
            }
            else {
                Debug.Assert(MathTool.IsPowerOfTwo(image.Size.X));
                Debug.Assert(MathTool.IsPowerOfTwo(image.Size.Y));
                if(material.Texture is Texture2D currentTex
                    && currentTex.Usage.HasFlag(TextureUsages.CopyDst)
                    && currentTex.Size == (Vector2u)image.Size) {

                    Debug.Assert(currentTex.Format == TextureFormat.Rgba8UnormSrgb);
                    Debug.Assert(currentTex.Usage.HasFlag(TextureUsages.CopyDst));
                    Debug.Assert(currentTex.MipLevelCount == 1);
                    currentTex.Write(0, image.GetPixels());
                    material.UpdateTextureContentSize(result.TextBoundsSize);
                }
                else {
                    var texture = Texture2D.CreateFromRawData(material.Shader.Screen, new()
                    {
                        Format = TextureFormat.Rgba8UnormSrgb,
                        MipLevelCount = 1,
                        SampleCount = 1,
                        Size = (Vector2u)image.Size,
                        Usage = TextureUsages.TextureBinding | TextureUsages.CopyDst,
                    }, image.GetPixels().AsBytes());
                    material.UpdateTexture(texture);
                    material.UpdateTextureContentSize(result.TextBoundsSize);
                }
            }
        });
    }
}
