#nullable enable
using System;

namespace Hikari.Imaging
{
    public interface IImageSource : IDisposable
    {
        int Width { get; }
        int Height { get; }
        unsafe ColorByte* Pixels { get; }
        short Token { get; }
        Span<ColorByte> GetPixels();
    }
}
