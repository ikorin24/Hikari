#nullable enable
using System;
using System.Diagnostics;
using System.Text.Json;

namespace Elffy.UI;

public readonly struct Brush
    : IFromJson<Brush>,
      IToJson,
      IEquatable<Brush>
{
    private readonly BrushType _type;
    private readonly Color4 _solidColor;

    public static Brush Transparent => new Brush(Color4.Transparent);
    public static Brush White => new Brush(Color4.White);
    public static Brush Black => new Brush(Color4.Black);

    public BrushType Type => _type;

    static Brush() => Serializer.RegisterConstructor(FromJson);

    private Brush(Color4 solidColor)
    {
        _type = BrushType.Solid;
        _solidColor = solidColor;
    }

    public bool TryGetSolidColor(out Color4 solidColor)
    {
        solidColor = _solidColor;
        return _type == BrushType.Solid;
    }

    public Color4 SolidColor
    {
        get
        {
            if(_type != BrushType.Solid) {
                ThrowHelper.ThrowInvalidOperation($"{nameof(Type)} is not {nameof(BrushType.Solid)}");
            }
            return _solidColor;
        }
    }

    public static Brush Solid(Color4 color)
    {
        return new Brush(color);
    }

    public static Brush FromJson(in ReactSource source)
    {
        // "#ffee23"
        // "red"
        switch(source.ValueKind) {
            case JsonValueKind.String: {
                var color = ExternalConstructor.Color4FromJson(source);
                return Solid(color);
            }
            default: {
                source.ThrowInvalidFormat();
                return default;
            }
        }
    }

    public JsonValueKind ToJson(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("@type", typeof(Brush).FullName);
        writer.WriteEnum(nameof(Type), _type);
        switch(_type) {
            case BrushType.Solid: {
                writer.Write(nameof(SolidColor), _solidColor);
                break;
            }
            default: {
                throw new UnreachableException();
            }
        }
        writer.WriteEndObject();
        return JsonValueKind.Object;
    }

    public override bool Equals(object? obj)
    {
        return obj is Brush brush && Equals(brush);
    }

    public bool Equals(Brush other)
    {
        return _type == other._type &&
               _solidColor.Equals(other._solidColor);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_type, _solidColor);
    }

    public static bool operator ==(Brush left, Brush right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Brush left, Brush right)
    {
        return !(left == right);
    }
}

public enum BrushType
{
    Solid = 0,
    //LinearGradient,
    //RadialGradient,
}
