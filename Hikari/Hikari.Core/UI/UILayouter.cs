#nullable enable

using System.Diagnostics;

namespace Hikari.UI;

internal static class UILayouter
{
    public static RectF DecideRect(in UIElementInfo target, in UIElementInfo parent, in RectF area, ref Vector2 flowHead)
    {
        const float xWidth = 200;  // TODO:
        const float yHeight = 100;  // TODO:
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
                    X = flowHead.X + margin.Left,
                    Y = CalcNoFlowY(target, area, fullSize, size),
                };
                flowHead.X = float.Max(flowHead.X, flowHead.X + size.X + margin.Left + margin.Right);
                break;
            }
            case { Direction: FlowDirection.Row, Wrap: FlowWrapMode.Wrap }: {
                var x = flowHead.X + margin.Left;
                if(x + size.X + margin.Right <= area.Position.X + area.Size.X) {
                    pos = new Vector2
                    {
                        X = x,
                        Y = flowHead.Y + margin.Top,
                    };
                    flowHead.X = float.Max(flowHead.X, flowHead.X + size.X + margin.Left + margin.Right);
                }
                else {
                    flowHead.Y += yHeight;
                    pos = new Vector2
                    {
                        X = area.Position.X + margin.Left,
                        Y = flowHead.Y + margin.Top,
                    };
                    flowHead.X = pos.X + size.X + margin.Right;
                }
                break;
            }
            case { Direction: FlowDirection.Column, Wrap: FlowWrapMode.NoWrap }: {
                pos = new Vector2
                {
                    X = CalcNoFlowX(target, area, fullSize, size),
                    Y = flowHead.Y + margin.Top,
                };
                flowHead.Y = float.Max(flowHead.Y, flowHead.Y + size.Y + margin.Top + margin.Bottom);
                break;
            }
            case { Direction: FlowDirection.Column, Wrap: FlowWrapMode.Wrap }: {
                var y = flowHead.Y + margin.Top;
                if(y + size.Y + margin.Bottom <= area.Position.Y + area.Size.Y) {
                    pos = new Vector2
                    {
                        X = flowHead.X + margin.Left,
                        Y = y,
                    };
                    flowHead.Y = float.Max(flowHead.Y, flowHead.Y + size.Y + margin.Top + margin.Bottom);
                }
                else {
                    flowHead.X += xWidth;
                    pos = new Vector2
                    {
                        X = flowHead.X + margin.Left,
                        Y = area.Position.Y + margin.Top,
                    };
                    flowHead.Y = pos.Y + size.Y + margin.Bottom;
                }
                break;
            }
            case { Direction: FlowDirection.RowReverse, Wrap: FlowWrapMode.NoWrap }: {
                pos = new Vector2
                {
                    X = flowHead.X - size.X - margin.Right,
                    Y = CalcNoFlowY(target, area, fullSize, size),
                };
                flowHead.X = float.Min(flowHead.X, flowHead.X - size.X - margin.Left - margin.Right);
                break;
            }
            case { Direction: FlowDirection.RowReverse, Wrap: FlowWrapMode.Wrap }: {
                var x = flowHead.X - size.X - margin.Right;
                if(x - margin.Left >= area.Position.X) {
                    pos = new Vector2
                    {
                        X = x,
                        Y = flowHead.Y + margin.Top,
                    };
                    flowHead.X = float.Min(flowHead.X, flowHead.X - size.X - margin.Left - margin.Right);
                }
                else {
                    flowHead.Y += yHeight;
                    pos = new Vector2
                    {
                        X = area.Position.X + area.Size.X - size.X - margin.Right,
                        Y = flowHead.Y + margin.Top,
                    };
                    flowHead.X = pos.X - margin.Left;
                }
                break;
            }
            case { Direction: FlowDirection.ColumnReverse, Wrap: FlowWrapMode.NoWrap }: {
                pos = new Vector2
                {
                    X = CalcNoFlowX(target, area, fullSize, size),
                    Y = flowHead.Y - size.Y - margin.Bottom,
                };
                flowHead.Y = float.Min(flowHead.Y, flowHead.Y - size.Y - margin.Top - margin.Bottom);
                break;
            }
            case { Direction: FlowDirection.ColumnReverse, Wrap: FlowWrapMode.Wrap }: {
                var y = flowHead.Y - size.Y - margin.Bottom;
                if(y - margin.Top >= area.Position.Y) {
                    pos = new Vector2
                    {
                        X = flowHead.X + margin.Left,
                        Y = y,
                    };
                    flowHead.Y = float.Min(flowHead.Y, flowHead.Y - size.Y - margin.Top - margin.Bottom);
                }
                else {
                    flowHead.X += xWidth;
                    pos = new Vector2
                    {
                        X = flowHead.X + margin.Left,
                        Y = area.Position.Y + area.Size.Y - size.Y - margin.Bottom,
                    };
                    flowHead.Y = pos.Y - margin.Top;
                }
                break;
            }
            case { Direction: FlowDirection.None } or _: {
                pos = new Vector2
                {
                    X = CalcNoFlowX(target, area, fullSize, size),
                    Y = CalcNoFlowY(target, area, fullSize, size),
                };
                break;
            }
        }
        return new RectF(pos, size);

        static float CalcNoFlowX(in UIElementInfo target, in RectF area, in Vector2 fullSize, in Vector2 size)
        {
            return target.HorizontalAlignment switch
            {
                HorizontalAlignment.Left => target.Margin.Left,
                HorizontalAlignment.Right => area.Size.X - target.Margin.Right - size.X,
                HorizontalAlignment.Center or _ => target.Margin.Left + (fullSize.X - size.X) / 2f,
            } + area.Position.X;
        }

        static float CalcNoFlowY(in UIElementInfo target, in RectF area, in Vector2 fullSize, in Vector2 size)
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
