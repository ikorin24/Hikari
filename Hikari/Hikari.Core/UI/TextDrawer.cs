#nullable enable
using Hikari.Imaging;
using Hikari.Collections;
using System;
using SkiaSharp;
using System.Diagnostics.CodeAnalysis;
using Hikari.Mathematics;

namespace Hikari.UI;

internal static class TextDrawer
{
    [ThreadStatic]
    private static SKTextBlobBuilder? _textBlobBuilderCache;

    [ThreadStatic]
    private static SKPaint? _paintCache;

    public static void Draw<T>(ReadOnlySpan<byte> utf8Text, in TextDrawOptions options, T arg, TextDrawerCallback<T> callback)
    {
        using var result = DrawPrivate(utf8Text, SKTextEncoding.Utf8, options);
        var a = new TextDrawerResult<T>
        {
            Arg = arg,
            Image = result.Image,
            TextBoundsSize = result.TextBounds,
        };
        callback.Invoke(a);
    }

    public static void Draw<T>(ReadOnlySpan<char> text, in TextDrawOptions options, T arg, TextDrawerCallback<T> callback)
    {
        using var result = DrawPrivate(text.MarshalCast<char, byte>(), SKTextEncoding.Utf16, options);
        var a = new TextDrawerResult<T>
        {
            Arg = arg,
            Image = result.Image,
            TextBoundsSize = result.TextBounds,
        };
        callback.Invoke(a);
    }

    private static unsafe DrawResult DrawPrivate(ReadOnlySpan<byte> text, SKTextEncoding enc, in TextDrawOptions options)
    {
        var skFont = options.Font;

        var glyphCount = skFont.CountGlyphs(text, enc);
        if(glyphCount == 0) {
            return DrawResult.None;
        }

        // Use cached instance to avoid GC
        var builder = (_textBlobBuilderCache ??= new SKTextBlobBuilder());
        var paint = (_paintCache ??= new SKPaint());

        paint.Reset();
        var foreground = options.Foreground;
        paint.Color = new SKColor(foreground.R, foreground.G, foreground.B, foreground.A);
        paint.Style = SKPaintStyle.Fill;
        paint.TextAlign = SKTextAlign.Left;

        var buffer = builder.AllocatePositionedRun(skFont, glyphCount);
        var glyphs = buffer.GetGlyphSpan();
        skFont.GetGlyphs(text, enc, glyphs);
        var glyphPositions = buffer.GetPositionSpan();
        skFont.MeasureText(glyphs, out var bounds, paint);
        skFont.GetGlyphPositions(glyphs, glyphPositions);

        float textWidth;
        {
            const int Threshold = 128;
            if(glyphCount <= Threshold) {
                float* s = stackalloc float[Threshold];
                var widths = new Span<float>(s, glyphCount);
                skFont.GetGlyphWidths(glyphs, widths, Span<SKRect>.Empty, paint);   // Set Span<SKRect>.Empty if it is not needed. (That is valid.)
                textWidth = glyphPositions[^1].X + widths[^1];
            }
            else {
                using var m = new ValueTypeRentMemory<float>(glyphCount, false);
                var widths = m.AsSpan();
                skFont.GetGlyphWidths(glyphs, widths, Span<SKRect>.Empty, paint);   // Set Span<SKRect>.Empty if it is not needed. (That is valid.)
                textWidth = glyphPositions[^1].X + widths[^1];
            }
        }
        skFont.GetFontMetrics(out var metrics);
        var size = new Vector2u
        {
            X = uint.Max(1, (uint)MathF.Ceiling(textWidth)),
            Y = uint.Max(1, (uint)MathF.Ceiling(metrics.Descent - metrics.Ascent + 2f * metrics.Leading)),
        };

        const SKColorType ColorType = SKColorType.Rgba8888;

        var info = new SKImageInfo(
            options.PowerOfTwoSizeRequired ? (int)MathTool.RoundUpToPowerOfTwo(size.X) : (int)size.X,
            options.PowerOfTwoSizeRequired ? (int)MathTool.RoundUpToPowerOfTwo(size.Y) : (int)size.Y,
            ColorType,
            SKAlphaType.Unpremul);


        var bitmap = new SKBitmap(info, SKBitmapAllocFlags.None);
        var canvas = new SKCanvas(bitmap);
        try {
            var image = new ImageViewMut((ColorByte*)bitmap.GetPixels(), info.Width, info.Height);
            image.GetPixels().Fill(options.Background);

            using(var textBlob = builder.Build()) {
                var x = 0;
                var y = -(metrics.Ascent + metrics.Leading);
                canvas.DrawText(textBlob, x, y, paint);
            }
            return new DrawResult
            {
                Bitmap = bitmap,
                Canvas = canvas,
                Image = image,
                TextBounds = size,
            };
        }
        catch {
            bitmap.Dispose();
            canvas.Dispose();
            throw;
        }
    }

    private readonly ref struct DrawResult
    {
        public static DrawResult None => default;

        [DisallowNull]
        public required SKBitmap? Bitmap { private get; init; }
        [DisallowNull]
        public required SKCanvas? Canvas { private get; init; }
        public required ImageView Image { get; init; }
        public required Vector2u TextBounds { get; init; }

        public bool IsNone => Image.IsEmpty;

        public void Dispose()
        {
            Bitmap?.Dispose();
            Canvas?.Dispose();
        }
    }
}

internal delegate void TextDrawerCallback<T>(TextDrawerResult<T> result);

internal readonly ref struct TextDrawerResult<T>
{
    public required T Arg { get; init; }
    public required ImageView Image { get; init; }
    public required Vector2u TextBoundsSize { get; init; }
}

internal readonly struct TextDrawOptions
{
    public required SKFont Font { get; init; }
    public required bool PowerOfTwoSizeRequired { get; init; }
    public required ColorByte Foreground { get; init; }
    public required ColorByte Background { get; init; }
}
