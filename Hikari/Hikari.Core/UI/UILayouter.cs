#nullable enable

using System.Diagnostics;

namespace Hikari.UI;

internal static class UILayouter
{
    public static LayoutResult Relayout(
        in UIElementInfo info,
        in UIElementInfo parent,
        in RectF parentContentArea,
        in FlowLayoutInfo flowInfo,
        float scaleFactor,
        out FlowLayoutInfo flowInfoOut)
    {
        flowInfoOut = flowInfo;
        var rect = DecideRect(info, parent, parentContentArea, scaleFactor, ref flowInfoOut);
        var desiredBorderRadius = new CornerRadius
        {
            TopLeft = info.BorderRadius.TopLeft * scaleFactor,
            TopRight = info.BorderRadius.TopRight * scaleFactor,
            BottomRight = info.BorderRadius.BottomRight * scaleFactor,
            BottomLeft = info.BorderRadius.BottomLeft * scaleFactor,
        };
        var layoutResult = new LayoutResult
        {
            Rect = rect,
            BorderRadius = DecideBorderRadius(desiredBorderRadius, rect.Size),
        };
        return layoutResult;
    }

    private static RectF DecideRect(in UIElementInfo target, in UIElementInfo parent, in RectF area, float scaleFactor, ref FlowLayoutInfo flowInfo)
    {
        Debug.Assert(area.Size.X >= 0 && area.Size.Y >= 0);

        ref var flowHead = ref flowInfo.FlowHead;
        var margin = new Thickness
        {
            Top = target.Margin.Top * scaleFactor,
            Right = target.Margin.Right * scaleFactor,
            Bottom = target.Margin.Bottom * scaleFactor,
            Left = target.Margin.Left * scaleFactor,
        };
        var fullSize = Vector2.Max(
            Vector2.Zero,
            area.Size - new Vector2(margin.Left + margin.Right, margin.Top + margin.Bottom));
        var sizeCoeff = new Vector2
        {
            X = target.Width.Type switch
            {
                LayoutLengthType.Proportion => area.Size.X,
                LayoutLengthType.Length or _ => scaleFactor,
            },
            Y = target.Height.Type switch
            {
                LayoutLengthType.Proportion => area.Size.Y,
                LayoutLengthType.Length or _ => scaleFactor,
            },
        };
        var size = new Vector2
        {
            X = float.Clamp(target.Width.Value * sizeCoeff.X, 0, fullSize.X),
            Y = float.Clamp(target.Height.Value * sizeCoeff.Y, 0, fullSize.Y),
        };

        Vector2 pos;
        switch(parent.Flow) {
            case { Direction: FlowDirection.Row, Wrap: FlowWrapMode.NoWrap }: {
                pos = new Vector2
                {
                    X = flowHead.X + margin.Left,
                    Y = CalcNoFlowY(target.VerticalAlignment, margin, area, fullSize, size),
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
                    flowInfo.NextLineOffset = float.Max(flowInfo.NextLineOffset, size.Y + margin.Top + margin.Bottom);
                }
                else {
                    flowHead.Y += flowInfo.NextLineOffset;
                    pos = new Vector2
                    {
                        X = area.Position.X + margin.Left,
                        Y = flowHead.Y + margin.Top,
                    };
                    flowHead.X = pos.X + size.X + margin.Right;
                    flowInfo.NextLineOffset = size.Y + margin.Top + margin.Bottom;
                }
                break;
            }
            case { Direction: FlowDirection.Row, Wrap: FlowWrapMode.WrapReverse }: {
                var x = flowHead.X + margin.Left;
                if(x + size.X + margin.Right <= area.Position.X + area.Size.X) {
                    pos = new Vector2
                    {
                        X = x,
                        Y = flowHead.Y - size.Y - margin.Bottom,
                    };
                    flowHead.X = float.Max(flowHead.X, flowHead.X + size.X + margin.Left + margin.Right);
                    flowInfo.NextLineOffset = float.Min(flowInfo.NextLineOffset, -size.Y - margin.Top - margin.Bottom);
                }
                else {
                    flowHead.Y += flowInfo.NextLineOffset;
                    pos = new Vector2
                    {
                        X = area.Position.X + margin.Left,
                        Y = flowHead.Y - size.Y - margin.Bottom,
                    };
                    flowHead.X = pos.X + size.X + margin.Right;
                    flowInfo.NextLineOffset = -size.Y - margin.Top - margin.Bottom;
                }
                break;
            }
            case { Direction: FlowDirection.Column, Wrap: FlowWrapMode.NoWrap }: {
                pos = new Vector2
                {
                    X = CalcNoFlowX(target.HorizontalAlignment, margin, area, fullSize, size),
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
                    flowInfo.NextLineOffset = float.Max(flowInfo.NextLineOffset, size.X + margin.Left + margin.Right);
                }
                else {
                    flowHead.Y += flowInfo.NextLineOffset;
                    pos = new Vector2
                    {
                        X = flowHead.X + margin.Left,
                        Y = area.Position.Y + margin.Top,
                    };
                    flowHead.Y = pos.Y + size.Y + margin.Bottom;
                    flowInfo.NextLineOffset = size.X + margin.Left + margin.Right;
                }
                break;
            }
            case { Direction: FlowDirection.Column, Wrap: FlowWrapMode.WrapReverse }: {
                var y = flowHead.Y + margin.Top;
                if(y + size.Y + margin.Bottom <= area.Position.Y + area.Size.Y) {
                    pos = new Vector2
                    {
                        X = flowHead.X - size.X - margin.Right,
                        Y = y,
                    };
                    flowHead.Y = float.Max(flowHead.Y, flowHead.Y + size.Y + margin.Top + margin.Bottom);
                    flowInfo.NextLineOffset = float.Min(flowInfo.NextLineOffset, -size.X - margin.Left - margin.Right);
                }
                else {
                    flowHead.X += flowInfo.NextLineOffset;
                    pos = new Vector2
                    {
                        X = flowHead.X - size.X - margin.Right,
                        Y = area.Position.Y + margin.Top,
                    };
                    flowHead.Y = pos.Y + size.Y + margin.Bottom;
                    flowInfo.NextLineOffset = -size.X - margin.Left - margin.Right;
                }
                break;
            }
            case { Direction: FlowDirection.RowReverse, Wrap: FlowWrapMode.NoWrap }: {
                pos = new Vector2
                {
                    X = flowHead.X - size.X - margin.Right,
                    Y = CalcNoFlowY(target.VerticalAlignment, margin, area, fullSize, size),
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
                    flowInfo.NextLineOffset = float.Max(flowInfo.NextLineOffset, size.Y + margin.Top + margin.Bottom);
                }
                else {
                    flowHead.Y += flowInfo.NextLineOffset;
                    pos = new Vector2
                    {
                        X = area.Position.X + area.Size.X - size.X - margin.Right,
                        Y = flowHead.Y + margin.Top,
                    };
                    flowHead.X = pos.X - margin.Left;
                    flowInfo.NextLineOffset = size.Y + margin.Top + margin.Bottom;
                }
                break;
            }
            case { Direction: FlowDirection.RowReverse, Wrap: FlowWrapMode.WrapReverse }: {
                var x = flowHead.X - size.X - margin.Right;
                if(x - margin.Left >= area.Position.X) {
                    pos = new Vector2
                    {
                        X = x,
                        Y = flowHead.Y - size.Y - margin.Bottom,
                    };
                    flowHead.X = float.Min(flowHead.X, flowHead.X - size.X - margin.Left - margin.Right);
                    flowInfo.NextLineOffset = float.Min(flowInfo.NextLineOffset, -size.Y - margin.Top - margin.Bottom);
                }
                else {
                    flowHead.Y += flowInfo.NextLineOffset;
                    pos = new Vector2
                    {
                        X = area.Position.X + area.Size.X - size.X - margin.Right,
                        Y = flowHead.Y - size.Y - margin.Bottom,
                    };
                    flowHead.X = pos.X - margin.Left;
                    flowInfo.NextLineOffset = -size.Y - margin.Top - margin.Bottom;
                }
                break;
            }
            case { Direction: FlowDirection.ColumnReverse, Wrap: FlowWrapMode.NoWrap }: {
                pos = new Vector2
                {
                    X = CalcNoFlowX(target.HorizontalAlignment, margin, area, fullSize, size),
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
                    flowInfo.NextLineOffset = float.Max(flowInfo.NextLineOffset, size.X + margin.Left + margin.Right);
                }
                else {
                    flowHead.X += flowInfo.NextLineOffset;
                    pos = new Vector2
                    {
                        X = flowHead.X + margin.Left,
                        Y = area.Position.Y + area.Size.Y - size.Y - margin.Bottom,
                    };
                    flowHead.Y = pos.Y - margin.Top;
                    flowInfo.NextLineOffset = size.X + margin.Left + margin.Right;
                }
                break;
            }
            case { Direction: FlowDirection.ColumnReverse, Wrap: FlowWrapMode.WrapReverse }: {
                var y = flowHead.Y - size.Y - margin.Bottom;
                if(y - margin.Top >= area.Position.Y) {
                    pos = new Vector2
                    {
                        X = flowHead.X - size.X - margin.Right,
                        Y = y,
                    };
                    flowHead.Y = float.Min(flowHead.Y, flowHead.Y - size.Y - margin.Top - margin.Bottom);
                    flowInfo.NextLineOffset = float.Min(flowInfo.NextLineOffset, -size.X - margin.Left - margin.Right);
                }
                else {
                    flowHead.X += flowInfo.NextLineOffset;
                    pos = new Vector2
                    {
                        X = flowHead.X - size.X - margin.Right,
                        Y = area.Position.Y + area.Size.Y - size.Y - margin.Bottom,
                    };
                    flowHead.Y = pos.Y - margin.Top;
                    flowInfo.NextLineOffset = -size.X - margin.Left - margin.Right;
                }
                break;
            }
            case { Direction: FlowDirection.None } or _: {
                pos = new Vector2
                {
                    X = CalcNoFlowX(target.HorizontalAlignment, margin, area, fullSize, size),
                    Y = CalcNoFlowY(target.VerticalAlignment, margin, area, fullSize, size),
                };
                break;
            }
        }
        return new RectF(pos, size);

        static float CalcNoFlowX(HorizontalAlignment ha, in Thickness margin, in RectF area, in Vector2 fullSize, in Vector2 size)
        {
            return ha switch
            {
                HorizontalAlignment.Left => margin.Left,
                HorizontalAlignment.Right => area.Size.X - margin.Right - size.X,
                HorizontalAlignment.Center or _ => margin.Left + (fullSize.X - size.X) / 2f,
            } + area.Position.X;
        }

        static float CalcNoFlowY(VerticalAlignment va, in Thickness margin, in RectF area, in Vector2 fullSize, in Vector2 size)
        {
            return va switch
            {
                VerticalAlignment.Top => margin.Top,
                VerticalAlignment.Bottom => area.Size.Y - margin.Bottom - size.Y,
                VerticalAlignment.Center or _ => margin.Top + (fullSize.Y - size.Y) / 2f,
            } + area.Position.Y;
        }
    }

    private static Vector4 DecideBorderRadius(in CornerRadius desiredBorderRadius, in Vector2 actualSize)
    {
        var ratio0 = actualSize.X / (desiredBorderRadius.TopLeft + desiredBorderRadius.TopRight);
        var ratio1 = actualSize.Y / (desiredBorderRadius.TopRight + desiredBorderRadius.BottomRight);
        var ratio2 = actualSize.X / (desiredBorderRadius.BottomRight + desiredBorderRadius.BottomLeft);
        var ratio3 = actualSize.Y / (desiredBorderRadius.BottomLeft + desiredBorderRadius.TopLeft);

        // min(1f, ratio0, ratio1, ratio2, ratio3)
        var ratio = float.Min(1f, float.Min(float.Min(ratio0, ratio1), float.Min(ratio2, ratio3)));
        return desiredBorderRadius.ToVector4() * ratio;
    }
}

internal struct FlowLayoutInfo
{
    public required Vector2 FlowHead;
    public required float NextLineOffset;
}
