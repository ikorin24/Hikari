#nullable enable

using System.Diagnostics;

namespace Hikari.UI;

internal static class UILayouter
{
    public static RectF DecideRect(in UIElementInfo target, in UIElementInfo parent, in RectF area, ref Vector2 flowHead)
    {
        Debug.Assert(area.Size.X >= 0 && area.Size.Y >= 0);

        var margin = target.Margin;
        var width = target.Width;
        var height = target.Height;
        var blankSize = new Vector2
        {
            X = margin.Left + margin.Right,
            Y = margin.Top + margin.Bottom,
        };
        var fullSize = Vector2.Max(Vector2.Zero, area.Size - blankSize);
        var sizeCoeff = new Vector2
        {
            X = width.Type switch
            {
                LayoutLengthType.Proportion => area.Size.X,
                LayoutLengthType.Length or _ => 1f,
            },
            Y = height.Type switch
            {
                LayoutLengthType.Proportion => area.Size.Y,
                LayoutLengthType.Length or _ => 1f,
            },
        };
        var size = new Vector2
        {
            X = float.Clamp(width.Value * sizeCoeff.X, 0, fullSize.X),
            Y = float.Clamp(height.Value * sizeCoeff.Y, 0, fullSize.Y),
        };

        Vector2 pos;
        switch(parent.Flow) {
            case { Direction: FlowDirection.Row, Wrap: FlowWrapMode.NoWrap }: {
                pos = new Vector2
                {
                    X = flowHead.X,
                    Y = CalcDefaultY(target, area, fullSize, size),
                };
                flowHead.X = float.Max(flowHead.X, flowHead.X + size.X + margin.Left + margin.Right);
                break;
            }
            case { Direction: FlowDirection.Row, Wrap: FlowWrapMode.Wrap }: {
                throw new System.NotImplementedException();
                //break;
            }
            case { Direction: FlowDirection.Column, Wrap: FlowWrapMode.NoWrap }: {
                pos = new Vector2
                {
                    X = CalcDefaultX(target, area, fullSize, size),
                    Y = flowHead.Y,
                };
                flowHead.Y = float.Max(flowHead.Y, flowHead.Y + size.Y + margin.Top + margin.Bottom);
                break;
            }
            case { Direction: FlowDirection.Column, Wrap: FlowWrapMode.Wrap }: {
                throw new System.NotImplementedException();
                //break;
            }

            case { Direction: FlowDirection.RowReverse, Wrap: FlowWrapMode.NoWrap }: {
                pos = new Vector2
                {
                    X = flowHead.X - size.X,
                    Y = CalcDefaultY(target, area, fullSize, size),
                };
                flowHead.X = float.Min(flowHead.X, flowHead.X - size.X - margin.Left - margin.Right);
                break;
            }
            case { Direction: FlowDirection.RowReverse, Wrap: FlowWrapMode.Wrap }: {
                throw new System.NotImplementedException();
                //break;
            }
            case { Direction: FlowDirection.ColumnReverse, Wrap: FlowWrapMode.NoWrap }: {
                pos = new Vector2
                {
                    X = CalcDefaultX(target, area, fullSize, size),
                    Y = flowHead.Y - size.Y,
                };
                flowHead.Y = float.Min(flowHead.Y, flowHead.Y - size.Y - margin.Top - margin.Bottom);
                break;
            }
            case { Direction: FlowDirection.ColumnReverse, Wrap: FlowWrapMode.Wrap }: {
                throw new System.NotImplementedException();
                //break;
            }
            case { Direction: FlowDirection.None } or _: {
                pos = new Vector2
                {
                    X = CalcDefaultX(target, area, fullSize, size),
                    Y = CalcDefaultY(target, area, fullSize, size),
                };
                break;
            }
        }
        return new RectF(pos, size);

        static float CalcDefaultX(in UIElementInfo target, in RectF area, in Vector2 fullSize, in Vector2 size)
        {
            return target.HorizontalAlignment switch
            {
                HorizontalAlignment.Left => target.Margin.Left,
                HorizontalAlignment.Right => area.Size.X - target.Margin.Right - size.X,
                HorizontalAlignment.Center or _ => target.Margin.Left + (fullSize.X - size.X) / 2f,
            } + area.Position.X;
        }

        static float CalcDefaultY(in UIElementInfo target, in RectF area, in Vector2 fullSize, in Vector2 size)
        {
            return target.VerticalAlignment switch
            {
                VerticalAlignment.Top => target.Margin.Top,
                VerticalAlignment.Bottom => area.Size.Y - target.Margin.Bottom - size.Y,
                VerticalAlignment.Center or _ => target.Margin.Top + (fullSize.Y - size.Y) / 2f,
            } + area.Position.Y;
        }
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
