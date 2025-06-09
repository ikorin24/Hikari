#nullable enable
using System.Diagnostics;

namespace Hikari.UI;

internal static class TextMaterialHelper
{
    public static (MaybeOwn<Texture2D> NewTexture, Vector2u ContentSize, bool Changed) UpdateTextTexture(Screen screen, Texture2D? texture, in UpdateTextTextureArg arg)
    {
        using var font = arg.Typeface.CreateFont(arg.FontSize.Px * arg.ScaleFactor);
        var options = new TextDrawOptions
        {
            TextBackground = ColorByte.Transparent,
            RectBackground = ColorByte.Transparent,
            Foreground = arg.Color,
            Font = font,
            TextAlignment = arg.TextAlignment,
        };
        var drawArg = (screen, texture);
        return TextDrawer.Draw(arg.Text, options, drawArg, static result =>
        {
            var (screen, t) = result.Arg;
            var image = result.Image;
            if(image.Size.X == 0) {
                Debug.Assert(image.Size.Y == 0);
                var emptyTexture = UIShader.GetEmptyTexture2D(screen);
                return (emptyTexture, Vector2u.One, true);
            }
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
                return ((MaybeOwn<Texture2D>)newTexture, result.TextBoundsSize, true);
            }
        });
    }
}

internal readonly record struct UpdateTextTextureArg
{
    public required string Text { get; init; }
    public required FontSize FontSize { get; init; }
    public required ColorByte Color { get; init; }
    public required float ScaleFactor { get; init; }
    public required TextAlignment TextAlignment { get; init; }
    public required Typeface Typeface { get; init; }
}
