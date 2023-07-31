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
                    element.ModelAlive.Subscribe(model => component.OnMount());
                    element.ModelDead.Subscribe(model => component.OnUnmount());
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
                    element.ModelAlive.Subscribe(model => c.OnMount());
                    element.ModelDead.Subscribe(model => c.OnUnmount());
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
