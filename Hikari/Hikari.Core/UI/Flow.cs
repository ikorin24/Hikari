#nullable enable
using System;
using System.Text.Json;

namespace Hikari.UI;

public readonly record struct Flow
    : IFromJson<Flow>,
      IToJson
{
    public FlowDirection Direction { get; init; }
    public FlowWrapMode Wrap { get; init; }

    public static Flow Default => new()
    {
        Direction = FlowDirection.None,
        Wrap = FlowWrapMode.NoWrap,
    };

    static Flow() => Serializer.RegisterConstructor(FromJson);

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

    internal Vector2 CalcChildrenFlowHead(in RectF contentArea)
    {
        return Wrap switch
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
