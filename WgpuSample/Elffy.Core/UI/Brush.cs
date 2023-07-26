#nullable enable
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elffy.UI;

public sealed class Brush
    : IFromJson<Brush>,
      IToJson
{
    private readonly BrushType _type;
    private readonly Color4 _solidColor;

    public static Brush White { get; } = new Brush(Color4.White);
    public static Brush Black { get; } = new Brush(Color4.Black);

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

    public JsonNode? ToJson()
    {
        //throw new NotImplementedException();

        return _type switch
        {
            BrushType.Solid => _solidColor.ToJson(),
            _ => "#FFFFFF",
        };
    }
}

public enum BrushType
{
    Solid = 0,
    //LinearGradient,
    //RadialGradient,
}
