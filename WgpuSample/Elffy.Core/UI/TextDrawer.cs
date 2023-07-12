#nullable enable
using Elffy.Imaging;
using Elffy.Effective;
using System;
using SkiaSharp;

namespace Elffy.UI;

internal static class TextDrawer
{
    [ThreadStatic]
    private static SKTextBlobBuilder? _textBlobBuilderCache;

    [ThreadStatic]
    private static SKPaint? _paintCache;

    public static bool Draw<T>(ReadOnlySpan<byte> utf8Text, in TextDrawOptions options, T arg, ReadOnlyImageAction<T> callback)
    {
        using var result = DrawPrivate(utf8Text, SKTextEncoding.Utf8, options);
        if(result.IsNone) {
            return false;
        }
        else {
            callback.Invoke(result.Image, arg);
            return true;
        }
    }

    public static bool Draw<T>(ReadOnlySpan<char> text, in TextDrawOptions options, T arg, ReadOnlyImageAction<T> callback)
    {
        using var result = DrawPrivate(text.MarshalCast<char, byte>(), SKTextEncoding.Utf16, options);
        if(result.IsNone) {
            return false;
        }
        else {
            callback.Invoke(result.Image, arg);
            return true;
        }
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
        paint.IsAntialias = true;
        paint.SubpixelText = true;
        paint.LcdRenderText = true;
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
        var size = new Vector2i
        {
            X = (int)MathF.Ceiling(textWidth),
            Y = (int)MathF.Ceiling(metrics.Bottom - metrics.Top),
        };

        const SKColorType ColorType = SKColorType.Rgba8888;
        var info = new SKImageInfo(size.X, size.Y, ColorType, SKAlphaType.Unpremul);
        var bitmap = new SKBitmap(info, SKBitmapAllocFlags.None);
        var canvas = new SKCanvas(bitmap);
        try {
            var result = new ImageRef((ColorByte*)bitmap.GetPixels(), size.X, size.Y);
            result.GetPixels().Fill(options.Background);

            using(var textBlob = builder.Build()) {
                var x = 0;
                var y = -metrics.Top;
                canvas.DrawText(textBlob, x, y, paint);
            }
            return new DrawResult(bitmap, canvas, result);
        }
        catch {
            bitmap.Dispose();
            canvas.Dispose();
            throw;
        }
    }

    private readonly ref struct DrawResult
    {
        private readonly SKBitmap? _bitmap;
        private readonly SKCanvas? _canvas;
        private readonly ReadOnlyImageRef _result;

        public static DrawResult None => default;

        public ReadOnlyImageRef Image => _result;

        public bool IsNone => _result.IsEmpty;

        [Obsolete("Don't use default constructor.", true)]
        public DrawResult() => throw new NotSupportedException("Don't use default constructor.");

        public DrawResult(SKBitmap bitmap, SKCanvas canvas, ReadOnlyImageRef result)
        {
            _bitmap = bitmap;
            _canvas = canvas;
            _result = result;
        }

        public void Dispose()
        {
            _bitmap?.Dispose();
            _canvas?.Dispose();
        }
    }
}

internal readonly struct TextDrawOptions
{
    public SKFont Font { get; init; }
    //public Vector2 TargetSize { get; init; }
    //public HorizontalTextAlignment Alignment { get; init; }
    public ColorByte Foreground { get; init; }
    public ColorByte Background { get; init; }
}


internal static class A
{
    public static void Hoge<T>(ReadOnlySpan<char> text, T arg, ReadOnlyImageAction<T> callback)
    {
        using var font = new SKFont();
        font.Size = 13;
        font.Subpixel = true;
        var options = new TextDrawOptions
        {
            Font = font,
            Background = ColorByte.Transparent,
            Foreground = ColorByte.Red,
        };
        TextDrawer.Draw(text, options, "", (image, _) =>
        {
            // do something
        });
    }
}
