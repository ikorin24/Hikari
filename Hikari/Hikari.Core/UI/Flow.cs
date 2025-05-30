#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Hikari.UI;

public readonly record struct Flow
#if HIKARI_JSON_SERDE
    : IFromJson<Flow>,
      IToJson
#endif
{
    public required FlowDirection Direction { get; init; }
    public FlowWrapMode Wrap { get; init; }

    public static Flow Default => new()
    {
        Direction = FlowDirection.None,
        Wrap = FlowWrapMode.NoWrap,
    };

#if HIKARI_JSON_SERDE
    static Flow() => Serializer.RegisterConstructor(FromJson);
#endif

    [SetsRequiredMembers]
    public Flow(FlowDirection direction)
    {
        Direction = direction;
        Wrap = FlowWrapMode.NoWrap;
    }

    [SetsRequiredMembers]
    public Flow(FlowDirection direction, FlowWrapMode wrap)
    {
        Direction = direction;
        Wrap = wrap;
    }


#if HIKARI_JSON_SERDE
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
#endif

    internal FlowLayoutInfo NewChildrenFlowInfo(in RectF contentArea)
    {
        var flowHead = Wrap switch
        {
            FlowWrapMode.NoWrap or FlowWrapMode.Wrap => Direction switch
            {
                FlowDirection.Row => new Vector2(contentArea.X, contentArea.Y),
                FlowDirection.Column => new Vector2(contentArea.X, contentArea.Y),
                FlowDirection.RowReverse => new Vector2(contentArea.X + contentArea.Width, contentArea.Y),
                FlowDirection.ColumnReverse => new Vector2(contentArea.X, contentArea.Y + contentArea.Height),
                FlowDirection.None or _ => Vector2.Zero,
            },
            FlowWrapMode.WrapReverse => Direction switch
            {
                FlowDirection.Row => new Vector2(contentArea.X, contentArea.Y + contentArea.Height),
                FlowDirection.Column => new Vector2(contentArea.X + contentArea.Width, contentArea.Y),
                FlowDirection.RowReverse => new Vector2(contentArea.X + contentArea.Width, contentArea.Y + contentArea.Height),
                FlowDirection.ColumnReverse => new Vector2(contentArea.X + contentArea.Width, contentArea.Y + contentArea.Height),
                FlowDirection.None or _ => Vector2.Zero,
            },
            _ => Vector2.Zero,
        };
        return new FlowLayoutInfo
        {
            FlowHead = flowHead,
            NextLineOffset = 0f,
        };
    }
}

public enum FlowDirection
{
    None = 0,
    Row,
    Column,
    RowReverse,
    ColumnReverse,
}

public enum FlowWrapMode
{
    NoWrap = 0,
    Wrap,
    WrapReverse,
}
