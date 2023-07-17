#nullable enable
using System;
using System.Diagnostics;
using System.Text.Json;

namespace Elffy.UI;

internal static class UIComponentBuilder
{
    public static UIElement Build(this IUIComponent component)
    {
        var obj = component.Render().DeserializeAndClear();
        while(true) {
            switch(obj) {
                case UIElement element: {
                    Debug.Assert(element.Parent == null);
                    element.ModelUpdate.Subscribe(model =>
                    {
                        if(component.NeedsToRerender) {
                            component.Apply(model.Element);
                        }
                    });
                    return element;
                }
                case IUIComponent c: {
                    return c.Build();
                }
                default: {
                    throw new NotSupportedException();
                }
            }
        }
    }

    public static void Apply(this IUIComponent component, IReactive target)
    {
        var source = component.Render();
        using var json = JsonDocument.Parse(source.ToStringAndClear(out var data));
        target.ApplyDiff(json.RootElement, data);
    }
}
