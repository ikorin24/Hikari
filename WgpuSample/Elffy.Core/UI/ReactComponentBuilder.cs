#nullable enable
using System;
using System.Diagnostics;
using System.Text.Json;

namespace Elffy.UI;

internal static class ReactComponentBuilder
{
    public static UIElement Build(this IReactComponent component)
    {
        var source = component.GetReactSource();
        var obj = source.Instantiate<object>();
        while(true) {
            switch(obj) {
                case UIElement element: {
                    Debug.Assert(element.Parent == null);
                    element.ModelEarlyUpdate.Subscribe(model =>
                    {
                        if(component.NeedsToRerender) {
                            component.Apply(model.Element);
                        }
                    });
                    component.RenderCompleted();
                    return element;
                }
                case IReactComponent c: {
                    var element = c.Build();
                    component.RenderCompleted();
                    return element;
                }
                default: {
                    throw new ArgumentException($"invalid object type: {obj.GetType()}");
                }
            }
        }
    }

    private static void Apply(this IReactComponent component, IReactive target)
    {
        var source = component.GetReactSource();
        target.ApplyDiff(source);
        component.RenderCompleted();
    }
}
