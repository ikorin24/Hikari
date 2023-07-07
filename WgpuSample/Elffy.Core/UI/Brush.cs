#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elffy.UI;

public sealed class Brush
    : IFromJson<Brush>,
      IToJson
{
    private readonly BrushType _type;
    private readonly Color4 _solidColor;

    internal static Brush Default { get; } = new Brush(Color4.White);

    public BrushType Type => _type;

    static Brush() => Serializer.RegisterConstructor(FromJson);

    private Brush(Color4 solidColor)
    {
        _type = BrushType.Solid;
        _solidColor = solidColor;
    }

    public bool TryGetSolidColorr(out Color4 solidColor)
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

    public static Brush FromJson(JsonElement element)
    {
        // "#ffee23"
        // "red"
        switch(element.ValueKind) {
            case JsonValueKind.String: {
                var str = element.GetStringNotNull();
                if(Color4.TryFromHexCode(str, out var color) || Color4.TryFromWebColorName(str, out color)) {
                    return Solid(color);
                }
                throw new FormatException($"invalid format: {str}");
            }
            default: {
                throw new FormatException($"invalid format: {element}");
            }
        }
    }

    public JsonNode? ToJson()
    {
        throw new NotImplementedException();

        //return _type switch
        //{
        //    BrushType.Solid => _solidColor,
        //    _ => "#FFFFFF",
        //};
    }
}

public enum BrushType
{
    Solid = 0,
    //LinearGradient,
    //RadialGradient,
}
