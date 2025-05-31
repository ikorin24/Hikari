#if HIKARI_JSON_SERDE
#nullable enable
using System;
using System.Text.Json;

namespace Hikari.UI;

partial struct BoxShadow : IFromJson<BoxShadow>, IToJson
{
    static partial void RegistorSerdeConstructor()
    {
        Serializer.RegisterConstructor(FromJson);
    }

    public static BoxShadow FromJson(in ObjectSource source)
    {
        switch(source.ValueKind) {
            case JsonValueKind.String: {
                // none
                // OffsetX OffsetY Color
                // OffsetX OffsetY BlurRadius Color
                // OffsetX OffsetY BlurRadius SpreadRadius Color
                // inset OffsetX OffsetY Color
                // inset OffsetX OffsetY BlurRadius Color
                // inset OffsetX OffsetY BlurRadius SpreadRadius Color

                var str = source.GetStringNotNull();
                if(str == "none") {
                    return None;
                }

                var splits = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                ReadOnlySpan<string> values;
                if(TryParseInset(splits[0], out var inset)) {
                    values = splits[1..];
                }
                else {
                    values = splits;
                    inset = false;
                }
                return values.Length switch
                {
                    3 => new BoxShadow
                    {
                        IsInset = inset,
                        OffsetX = ParseLength(values[0]),
                        OffsetY = ParseLength(values[1]),
                        Color = ParseColor(values[2]),
                    },
                    4 => new BoxShadow
                    {
                        IsInset = inset,
                        OffsetX = ParseLength(values[0]),
                        OffsetY = ParseLength(values[1]),
                        BlurRadius = ParseLength(values[2]),
                        Color = ParseColor(values[3]),
                    },
                    5 => new BoxShadow
                    {
                        IsInset = inset,
                        OffsetX = ParseLength(values[0]),
                        OffsetY = ParseLength(values[1]),
                        BlurRadius = ParseLength(values[2]),
                        SpreadRadius = ParseLength(values[3]),
                        Color = ParseColor(values[4]),
                    },
                    _ => throw new FormatException("Invalid BoxShadow format"),
                };
            }
            default: {
                source.ThrowInvalidFormat();
                return default;
            }
        }

        static bool TryParseInset(ReadOnlySpan<char> split, out bool inset)
        {
            if(split.SequenceEqual("inset")) {
                inset = true;
                return true;
            }
            inset = false;
            return false;
        }

        static float ParseLength(ReadOnlySpan<char> str)
        {
            if(str.EndsWith("px")) {
                return float.Parse(str[..^2]);
            }
            else {
                throw new FormatException("invalid length format");
            }
        }

        static Color4 ParseColor(ReadOnlySpan<char> str)
        {
            if(Color4.TryFromHexCode(str, out var color) || Color4.TryFromWebColorName(str, out color)) {
                return color;
            }
            else {
                throw new FormatException("invalid color format");
            }
        }
    }

    public JsonValueKind ToJson(Utf8JsonWriter writer)
    {
        if(this == None) {
            writer.WriteStringValue("none");
            return JsonValueKind.String;
        }
        else {
            if(IsInset) {
                writer.WriteStringValue($"inset {OffsetX}px {OffsetY}px {BlurRadius}px {SpreadRadius}px {Color.ToColorByte().ToHexCode()}");

            }
            else {
                writer.WriteStringValue($"{OffsetX}px {OffsetY}px {BlurRadius}px {SpreadRadius}px {Color.ToColorByte().ToHexCode()}");
            }
        }
        return JsonValueKind.String;
    }
}
#endif
