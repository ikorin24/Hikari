#if HIKARI_JSON_SERDE
#nullable enable
using System;
using System.Text.Json;

namespace Hikari.UI;

partial record struct Flow : IFromJson<Flow>, IToJson
{
    static partial void RegistorSerdeConstructor()
    {
        Serializer.RegisterConstructor(FromJson);
    }

    public JsonValueKind ToJson(Utf8JsonWriter writer)
    {
        if(Direction == FlowDirection.None) {
            writer.WriteStringValue(Direction.ToString());
            return JsonValueKind.String;
        }
        else {
            writer.WriteStringValue($"{Direction} {Wrap}");
            return JsonValueKind.String;
        }
    }

    public static Flow FromJson(in ObjectSource source)
    {
        // "<direction>"
        // "<direction> <wrap>"
        switch(source.ValueKind) {
            case JsonValueKind.String: {
                var value = source.GetStringNotNull().AsSpan();
                var (dir, wrap) = value.Split2(' ');
                if(wrap.IsEmpty || wrap.Trim().IsEmpty) {
                    return new Flow
                    {
                        Direction = Enum.Parse<FlowDirection>(dir),
                        Wrap = FlowWrapMode.NoWrap,
                    };
                }
                else {
                    return new Flow
                    {
                        Direction = Enum.Parse<FlowDirection>(dir),
                        Wrap = Enum.Parse<FlowWrapMode>(wrap),
                    };
                }
            }
            default: {
                source.ThrowInvalidFormat();
                return default;
            }
        }
    }
}
#endif
