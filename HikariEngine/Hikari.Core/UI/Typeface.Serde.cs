#if HIKARI_JSON_SERDE
#nullable enable
using System;
using System.Text.Json;

namespace Hikari.UI;

partial record struct Typeface : IFromJson<Typeface>, IToJson
{
    static partial void RegistorSerdeConstructor()
    {
        Serializer.RegisterConstructor(FromJson);
    }

    public JsonValueKind ToJson(Utf8JsonWriter writer)
    {
        switch(_sourceType) {
            case SourceType.Default: {
                writer.WriteStringValue($"{_sourceType}");
                return JsonValueKind.String;
            }
            case SourceType.FilePath: {
                writer.WriteStringValue($"{_sourceType} {_path}");
                return JsonValueKind.String;
            }
            default:
                throw new NotImplementedException($"soruceType: {_sourceType}");
        }
    }

    public static Typeface FromJson(in ObjectSource source)
    {
        if(source.ValueKind != JsonValueKind.String) {
            source.ThrowInvalidFormat();
        }

        // (ex)
        // "Default"
        // "FilePath path/to the font/bar.otf"
        var value = source.GetStringNotNull().AsSpan();
        var sourceTypeStr = value.Split2(' ').Item1;
        var sourceType = Enum.Parse<SourceType>(sourceTypeStr);
        switch(sourceType) {
            case SourceType.Default: {
                return Typeface.Default;
            }
            case SourceType.FilePath: {
                var path = value[(sourceTypeStr.Length + 1)..].ToString();
                return Typeface.FromFile(path);
            }
            default:
                throw new NotImplementedException($"soruceType: {sourceType}");
        }
    }
}
#endif
