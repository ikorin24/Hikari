#nullable enable
using System;
using System.Buffers;
using System.IO;
using Hikari.Imaging.Internal;
using SkiaSharp;

namespace Hikari.Imaging
{
    public static class ImageExtensions
    {
        public static Image ToSubimage(in this Image source, in RectI rect)
            => ToSubimage(source.AsView(), rect.X, rect.Y, rect.Width, rect.Height);

        public static Image ToSubimage(in this Image source, int x, int y, int width, int height)
            => ToSubimage(source.AsView(), x, y, width, height);

        public static Image ToSubimage(in this ImageViewMut source, in RectI rect)
            => ToSubimage(source.AsReadOnly(), rect.X, rect.Y, rect.Width, rect.Height);

        public static Image ToSubimage(in this ImageViewMut source, int x, int y, int width, int height)
            => ToSubimage(source.AsReadOnly(), x, y, width, height);

        public static Image ToSubimage(in this ImageView source, in RectI rect)
            => ToSubimage(source, rect.X, rect.Y, rect.Width, rect.Height);

        public static Image ToSubimage(in this ImageView source, int x, int y, int width, int height)
        {
            if((uint)x > source.Width) { ThrowHelper.ThrowArgOutOfRange(nameof(x)); }
            if((uint)y > source.Height) { ThrowHelper.ThrowArgOutOfRange(nameof(y)); }
            if((uint)width > source.Width - x) { ThrowHelper.ThrowArgOutOfRange(nameof(width)); }
            if((uint)height > source.Height - y) { ThrowHelper.ThrowArgOutOfRange(nameof(height)); }

            if(x == 0 && y == 0 && width == source.Width && height == source.Height) {
                return source.ToImage();
            }

            var subimage = new Image(width, height, false);
            try {
                for(int i = 0; i < height; i++) {
                    source.GetRowLine(y + i).Slice(x, width).CopyTo(subimage.GetRowLine(i));
                }
                return subimage;
            }
            catch {
                subimage.Dispose();
                throw;
            }
        }

        /// <summary>Copy the source image to the position in the destination image.</summary>
        /// <remarks>If outside of range of the dest, only the copyable part would be copied. No exceptions will be thrown.</remarks>
        /// <param name="source">source image</param>
        /// <param name="dest">destination image</param>
        /// <param name="posX">position x in the destination</param>
        /// <param name="posY">position y in the destination</param>
        public static void CopyTo(in this Image source, in ImageViewMut dest, int posX, int posY) => source.AsView().CopyTo(dest, new Vector2i(posX, posY));

        /// <summary>Copy the source image to the position in the destination image.</summary>
        /// <remarks>If outside of range of the dest, only the copyable part would be copied. No exceptions will be thrown.</remarks>
        /// <param name="source">source image</param>
        /// <param name="dest">destination image</param>
        /// <param name="pos">position in the destination</param>
        public static void CopyTo(in this Image source, in Image dest, in Vector2i pos) => source.AsView().CopyTo(dest, pos);

        /// <summary>Copy the source image to the position in the destination image.</summary>
        /// <remarks>If outside of range of the dest, only the copyable part would be copied. No exceptions will be thrown.</remarks>
        /// <param name="source">source image</param>
        /// <param name="dest">destination image</param>
        /// <param name="posX">position x in the destination</param>
        /// <param name="posY">position y in the destination</param>
        public static void CopyTo(in this ImageViewMut source, in ImageViewMut dest, int posX, int posY) => source.AsReadOnly().CopyTo(dest, new Vector2i(posX, posY));

        /// <summary>Copy the source image to the position in the destination image.</summary>
        /// <remarks>If outside of range of the dest, only the copyable part would be copied. No exceptions will be thrown.</remarks>
        /// <param name="source">source image</param>
        /// <param name="dest">destination image</param>
        /// <param name="pos">position in the destination</param>
        public static void CopyTo(in this ImageViewMut source, in ImageViewMut dest, in Vector2i pos) => source.AsReadOnly().CopyTo(dest, pos);

        /// <summary>Copy the source image to the position in the destination image.</summary>
        /// <remarks>If outside of range of the dest, only the copyable part would be copied. No exceptions will be thrown.</remarks>
        /// <param name="source">source image</param>
        /// <param name="dest">destination image</param>
        /// <param name="posX">position x in the destination</param>
        /// <param name="posY">position y in the destination</param>
        public static void CopyTo(in this ImageView source, in ImageViewMut dest, int posX, int posY) => source.CopyTo(dest, new Vector2i(posX, posY));

        /// <summary>Copy the source image to the position in the destination image.</summary>
        /// <remarks>If outside of range of the dest, only the copyable part would be copied. No exceptions will be thrown.</remarks>
        /// <param name="source">source image</param>
        /// <param name="dest">destination image</param>
        /// <param name="pos">position in the destination</param>
        public static void CopyTo(in this ImageView source, in ImageViewMut dest, in Vector2i pos)
        {
            // +---------------+
            // |      dest     |
            // |      +--------+------+
            // |      |////////|      |
            // +------+--------+      |
            //        |     source    |
            //        |               |
            //        +---------------+

            if(pos.X >= dest.Width && pos.X + source.Width < 0) {
                return;
            }
            var destRowStart = Math.Max(pos.Y, 0);
            var destRowEnd = Math.Min(pos.Y + source.Height, dest.Height);
            var destColStart = Math.Min(Math.Max(0, pos.X), dest.Width);
            var destColEnd = Math.Min(Math.Max(0, pos.X + source.Width), dest.Width);
            var widthToCopy = destColEnd - destColStart;
            var srcColStart = Math.Min(Math.Max(0, -pos.X), source.Width);

            for(int destRow = destRowStart; destRow < destRowEnd; destRow++) {
                var srcRow = destRow - destRowStart;
                var destRowLine = dest.GetRowLine(destRow).Slice(destColStart);
                var srcRowLine = source.GetRowLine(srcRow).Slice(srcColStart, widthToCopy);
                srcRowLine.CopyTo(destRowLine);
            }
        }

        public static void EncodeAsPng(this Image source, Stream streamToWrite) => EncodeAsPng(source.AsView(), streamToWrite);

        public static void EncodeAsPng(this ImageViewMut source, Stream streamToWrite) => EncodeAsPng(source.AsReadOnly(), streamToWrite);

        public static void EncodeAsPng(this ImageView source, Stream streamToWrite)
        {
            _ = EncodeImage(source, SKEncodedImageFormat.Png, streamToWrite, static (span, stream) =>
            {
                stream.Write(span);
                return stream;
            });
        }

        public static void EncodeAsPng(this Image source, IBufferWriter<byte> writer) => EncodeAsPng(source.AsView(), writer);

        public static void EncodeAsPng(this ImageViewMut source, IBufferWriter<byte> writer) => EncodeAsPng(source.AsReadOnly(), writer);

        public static void EncodeAsPng(this ImageView source, IBufferWriter<byte> writer)
        {
            _ = EncodeImage(source, SKEncodedImageFormat.Png, writer, static (span, writer) =>
            {
                writer.Write(span);
                return writer;
            });
        }

        public static byte[] EncodeAsPng(this Image source) => EncodeAsPng(source.AsView());
        public static byte[] EncodeAsPng(this ImageViewMut source) => EncodeAsPng(source.AsReadOnly());

        public static byte[] EncodeAsPng(this ImageView source)
        {
            return EncodeImage<object?, byte[]>(source, SKEncodedImageFormat.Png, null, (span, _) => span.ToArray());
        }

        public static unsafe void SaveAsPng(this Image source, string filePath) => SaveAsPng(source.AsView(), filePath);
        public static unsafe void SaveAsPng(this ImageViewMut source, string filePath) => SaveAsPng(source.AsReadOnly(), filePath);

        public static unsafe void SaveAsPng(this ImageView source, string filePath)
        {
            EncodeImage(source, SKEncodedImageFormat.Png, filePath, static (span, filePath) =>
            {
                if(File.Exists(filePath)) {
                    File.Delete(filePath);
                }
                using var handle = File.OpenHandle(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, preallocationSize: span.Length);
                RandomAccess.Write(handle, span, 0);
                return filePath;
            });
        }

        private static unsafe TResult EncodeImage<TState, TResult>(ImageView source, SKEncodedImageFormat format, TState state, ReadOnlySpanFunc<byte, TState, TResult> func)
        {
            fixed(void* ptr = source) {
                var info = new SKImageInfo(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
                using var pixmap = new SKPixmap(info, (IntPtr)ptr);
                using var data = pixmap.Encode(format, 0);
                return func.Invoke(data.AsSpan(), state);
            }
        }
    }
}
