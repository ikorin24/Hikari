#nullable enable
using Hikari.Imaging;
using Hikari.Collections;
using System;
using SkiaSharp;
using System.Collections.Generic;

namespace Hikari.UI;

internal static class TextDrawer
{
    private const SKColorType ColorType = SKColorType.Rgba8888;
    private const SKAlphaType AlphaType = SKAlphaType.Unpremul;

    [ThreadStatic]
    private static SKTextBlobBuilder? _textBlobBuilderCache;

    [ThreadStatic]
    private static SKPaint? _paintCache;

    public static TResult Draw<T, TResult>(ReadOnlySpan<byte> utf8Text, in TextDrawOptions options, T arg, TextDrawerCallback<T, TResult> callback)
    {
        using var result = DrawPrivate(utf8Text, SKTextEncoding.Utf8, options);
        var image = result.GetImage();
        var a = new TextDrawerResult<T>
        {
            Arg = arg,
            Image = image,
            TextBoundsSize = (Vector2u)image.Size,
        };
        return callback.Invoke(a);
    }

    public static TResult Draw<T, TResult>(ReadOnlySpan<char> text, in TextDrawOptions options, T arg, TextDrawerCallback<T, TResult> callback)
    {
        using var result = DrawPrivate(text.MarshalCast<char, byte>(), SKTextEncoding.Utf16, options);
        var image = result.GetImage();
        var a = new TextDrawerResult<T>
        {
            Arg = arg,
            Image = image,
            TextBoundsSize = (Vector2u)image.Size,
        };
        return callback.Invoke(a);
    }

    private static unsafe DrawResult DrawPrivate(ReadOnlySpan<byte> text, SKTextEncoding enc, in TextDrawOptions options)
    {
        if(text.IsEmpty) {
            return DrawResult.None;
        }

        var skFont = options.Font;

        // Use cached instance to avoid GC
        var builder = (_textBlobBuilderCache ??= new SKTextBlobBuilder());
        var paint = (_paintCache ??= new SKPaint());
        paint.Reset();
        var foreground = options.Foreground;
        paint.Color = new SKColor(foreground.R, foreground.G, foreground.B, foreground.A);
        paint.Style = SKPaintStyle.Fill;
        paint.TextAlign = SKTextAlign.Left;

        skFont.GetFontMetrics(out var metrics);
        var lineHeight = int.Max(1, (int)MathF.Ceiling(metrics.Descent - metrics.Ascent + 2f * metrics.Leading));

        var lineBitmaps = new List<SKBitmap>();
        switch(enc) {
            case SKTextEncoding.Utf8:
                foreach(var line in text.Split((byte)'\n')) {
                    var l = line;
                    if(l.Length > 0 && l[^1] == (byte)'\r') {
                        l = l[0..^1];
                    }
                    var lineBitmap = DrawSingleLine(l, enc, options, lineHeight, metrics.Ascent, builder, paint);
                    lineBitmaps.Add(lineBitmap);
                }
                break;
            case SKTextEncoding.Utf16:
                foreach(var lineChars in text.MarshalCast<byte, char>().EnumerateLines()) {
                    var lineBitmap = DrawSingleLine(lineChars.AsBytes(), enc, options, lineHeight, metrics.Ascent, builder, paint);
                    lineBitmaps.Add(lineBitmap);
                }
                break;
            default:
                throw new NotSupportedException($"encoding: {enc}");
        }

        var lineCount = lineBitmaps.Count;

        int width = 1;
        int height = 1;
        foreach(var lineBitmap in lineBitmaps) {
            width = Math.Max(width, lineBitmap.Width);
            height += lineBitmap.Height;
        }
        var info = new SKImageInfo(width, height, ColorType, AlphaType);
        SKBitmap? bitmap = null;
        try {
            if(options.RectBackground.A == 0) {
                bitmap = new SKBitmap(info, SKBitmapAllocFlags.ZeroPixels);
            }
            else {
                bitmap = new SKBitmap(info, SKBitmapAllocFlags.None);
                var image = new ImageViewMut((ColorByte*)bitmap.GetPixels(), info.Width, info.Height);
                image.GetPixels().Fill(options.RectBackground);
            }
            using var canvas = new SKCanvas(bitmap);


            switch(options.TextAlignment) {
                case TextAlignment.Center:
                default: {
                    int y = 0;
                    foreach(var lineBitmap in lineBitmaps) {
                        var x = (bitmap.Width - lineBitmap.Width) * 0.5f;
                        canvas.DrawBitmap(lineBitmap, new SKPoint(x, y));
                        y += lineBitmap.Height;
                    }
                    break;
                }
                case TextAlignment.Left: {
                    int y = 0;
                    foreach(var lineBitmap in lineBitmaps) {
                        canvas.DrawBitmap(lineBitmap, new SKPoint(0, y));
                        y += lineBitmap.Height;
                    }
                    break;
                }
                case TextAlignment.Right: {
                    int y = 0;
                    foreach(var lineBitmap in lineBitmaps) {
                        var x = bitmap.Width - lineBitmap.Width;
                        canvas.DrawBitmap(lineBitmap, new SKPoint(x, y));
                        y += lineBitmap.Height;
                    }
                    break;
                }
            }
        }
        catch {
            bitmap?.Dispose();
            throw;
        }
        return new DrawResult(bitmap, lineCount);
    }

    private static unsafe SKBitmap DrawSingleLine(ReadOnlySpan<byte> line, SKTextEncoding enc, in TextDrawOptions options,
        int lineHeight, float ascent, SKTextBlobBuilder builder, SKPaint paint)
    {
        var skFont = options.Font;

        var glyphCount = skFont.CountGlyphs(line, enc);
        if(glyphCount == 0) {
            return new SKBitmap(new SKImageInfo(1, lineHeight, ColorType, AlphaType), SKBitmapAllocFlags.ZeroPixels);
        }
        var buffer = builder.AllocatePositionedRun(skFont, glyphCount);
        var glyphs = buffer.GetGlyphSpan();
        skFont.GetGlyphs(line, enc, glyphs);
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
        var size = new Vector2i
        {
            X = int.Max(1, (int)MathF.Ceiling(textWidth)),
            Y = lineHeight,
        };
        var info = new SKImageInfo(size.X, size.Y, ColorType, AlphaType);
        var bitmap = new SKBitmap(info, SKBitmapAllocFlags.None);
        using(var canvas = new SKCanvas(bitmap)) {
            var image = new ImageViewMut((ColorByte*)bitmap.GetPixels(), info.Width, info.Height);
            image.GetPixels().Fill(options.TextBackground);
            using var textBlob = builder.Build();
            var x = 0;
            var y = -ascent;
            canvas.DrawText(textBlob, x, y, paint);
        }
        return bitmap;
    }

    private readonly ref struct DrawResult
    {
        public static DrawResult None => default;

        private readonly SKBitmap? _bitmap;
        private readonly int _lineCount;

        public int LineCount => _lineCount;

        public bool IsNone => _bitmap == null;

        public DrawResult(SKBitmap bitmap, int lineCount)
        {
            _bitmap = bitmap;
            _lineCount = lineCount;
        }

        public unsafe ImageView GetImage()
        {
            var bitmap = _bitmap;
            if(bitmap == null) {
                return ImageView.Empty;
            }
            else {
                return new ImageView((ColorByte*)bitmap.GetPixels(), bitmap.Width, bitmap.Height);
            }
        }

        public void Dispose()
        {
            _bitmap?.Dispose();
        }
    }
}

internal delegate TResult TextDrawerCallback<T, TResult>(TextDrawerResult<T> result);

internal readonly ref struct TextDrawerResult<T>
{
    public required T Arg { get; init; }
    public required ImageView Image { get; init; }
    public required Vector2u TextBoundsSize { get; init; }
}

internal readonly struct TextDrawOptions
{
    public required SKFont Font { get; init; }
    public required ColorByte Foreground { get; init; }
    public required ColorByte TextBackground { get; init; }
    public required ColorByte RectBackground { get; init; }
    public required TextAlignment TextAlignment { get; init; }
}
