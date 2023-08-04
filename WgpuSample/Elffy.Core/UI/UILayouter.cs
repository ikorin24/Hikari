#nullable enable

namespace Elffy.UI;

internal static class UILayouter
{
    public static RectF DecideRect(in UIElementInfo target, in ContentAreaInfo area)
    {
        var areaSize = Vector2.Max(Vector2.Zero, area.Rect.Size);

        var margin = target.Margin;
        var width = target.Width;
        var height = target.Height;
        var horizontalAlignment = target.HorizontalAlignment;
        var verticalAlignment = target.VerticalAlignment;

        var blankSize = new Vector2
        {
            X = area.Padding.Left + area.Padding.Right + margin.Left + margin.Right,
            Y = area.Padding.Top + area.Padding.Bottom + margin.Top + margin.Bottom,
        };
        var fullSize = Vector2.Max(Vector2.Zero, areaSize - blankSize);

        var sizeCoeff = new Vector2
        {
            X = width.Type switch
            {
                LayoutLengthType.Proportion => areaSize.X,
                LayoutLengthType.Length or _ => 1f,
            },
            Y = height.Type switch
            {
                LayoutLengthType.Proportion => areaSize.Y,
                LayoutLengthType.Length or _ => 1f,
            },
        };
        var size = new Vector2
        {
            X = float.Clamp(width.Value * sizeCoeff.X, 0, fullSize.X),
            Y = float.Clamp(height.Value * sizeCoeff.Y, 0, fullSize.Y),
        };

        var pos = new Vector2
        {
            X = horizontalAlignment switch
            {
                HorizontalAlignment.Left => area.Padding.Left + margin.Left,
                HorizontalAlignment.Right => areaSize.X - area.Padding.Right - margin.Right - size.X,
                HorizontalAlignment.Center or _ => area.Padding.Left + margin.Left + (fullSize.X - size.X) / 2f,
            },
            Y = verticalAlignment switch
            {
                VerticalAlignment.Top => area.Padding.Top + margin.Top,
                VerticalAlignment.Bottom => areaSize.Y - area.Padding.Bottom - margin.Bottom - size.Y,
                VerticalAlignment.Center or _ => area.Padding.Top + margin.Top + (fullSize.Y - size.Y) / 2f,
            },
        } + area.Rect.Position;
        return new RectF(pos, size);
    }

    public static Vector4 DecideBorderRadius(in CornerRadius desiredBorderRadius, in Vector2 actualSize)
    {
        var ratio0 = actualSize.X / (desiredBorderRadius.TopLeft + desiredBorderRadius.TopRight);
        var ratio1 = actualSize.Y / (desiredBorderRadius.TopRight + desiredBorderRadius.BottomRight);
        var ratio2 = actualSize.X / (desiredBorderRadius.BottomRight + desiredBorderRadius.BottomLeft);
        var ratio3 = actualSize.Y / (desiredBorderRadius.BottomLeft + desiredBorderRadius.TopLeft);
        var ratio = float.Min(1f, float.Min(float.Min(ratio0, ratio1), float.Min(ratio2, ratio3)));
        return desiredBorderRadius.ToVector4() * ratio;
    }
}
