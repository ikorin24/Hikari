#nullable enable
using System;
using System.Diagnostics;

namespace Elffy.UI;

internal static class ReactComponentBuilder
{
    public static UIElement BuildUIElement(this IReactComponent component)
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
                            var source = component.GetReactSource();
                            ((IReactive)model.Element).ApplyDiff(source);
                            component.RenderCompleted();
                        }
                    });
                    component.RenderCompleted();
                    return element;
                }
                case IReactComponent c: {
                    var element = c.BuildUIElement();
                    component.RenderCompleted();
                    return element;
                }
                default: {
                    throw new ArgumentException($"invalid object type: {obj.GetType()}");
                }
            }
        }
    }
}
