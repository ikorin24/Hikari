#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace Hikari.UI;

public readonly partial record struct Flow
{
    public required FlowDirection Direction { get; init; }
    public FlowWrapMode Wrap { get; init; }

    public static Flow Default => new()
    {
        Direction = FlowDirection.None,
        Wrap = FlowWrapMode.NoWrap,
    };

    static Flow() => RegistorSerdeConstructor();

    static partial void RegistorSerdeConstructor();

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
