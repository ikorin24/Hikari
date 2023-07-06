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

    public static Brush FromJson(JsonNode? node)
    {
        // "#ffee23"

        switch(node) {
            case JsonValue value: {
                var str = value.GetValue<string>();
                if(str.StartsWith("#")) {
                    var color = Color4.FromHexCode(str);    // TODO: use Color4.FromJson
                    return Solid(color);
                }
                else {
                    throw new FormatException();
                }
            }
            default: {
                throw new FormatException();
            }
        }
    }

    public JsonNode? ToJson(JsonSerializerOptions? options = null)
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
