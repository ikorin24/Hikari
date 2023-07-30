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
                            var target = (IReactive)model.Element;
                            target.ApplyDiff(source);
                            component.RenderCompleted(target);
                        }
                    });
                    component.RenderCompleted(element);
                    return element;
                }
                case IReactComponent c: {
                    var element = c.BuildUIElement();
                    component.RenderCompleted(c);
                    return element;
                }
                default: {
                    throw new ArgumentException($"invalid object type: {obj.GetType()}");
                }
            }
        }
    }
}
