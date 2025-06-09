#nullable enable
using SkiaSharp;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Hikari.UI;

public readonly partial record struct Typeface
{
    private static readonly Lock _lock = new();
    private static readonly ConditionalWeakTable<string, SKTypeface> _cache = new();

    private readonly string? _path;
    private readonly SourceType _sourceType;
    // Don't Dispose typeface. Leave it until it is automatically collected by the finalizer.
    private readonly SKTypeface? _typeface;

    public static Typeface Default => default;

    private Typeface(string path)
    {
        _path = path;
        _sourceType = SourceType.FilePath;
        SKTypeface? typeface;
        lock(_lock) {
            if(_cache.TryGetValue(path, out typeface) == false) {
                typeface = SKTypeface.FromFile(path);
                _cache.Add(path, typeface);
            }
        }
        _typeface = typeface;
    }

    static Typeface() => RegistorSerdeConstructor();

    static partial void RegistorSerdeConstructor();

    public static Typeface FromFile(string path)
    {
        return new Typeface(path);
    }

    internal SKFont CreateFont(float size)
    {
        if(_typeface == null) {
            var font = new SKFont();
            font.Size = size;
            return font;
        }
        else {
            return new SKFont(_typeface, size: size);
        }
    }

    private enum SourceType
    {
        Default = 0,
        FilePath,
    }
}
