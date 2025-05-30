#if HIKARI_JSON_SERDE
#nullable enable
using System;
using System.Diagnostics;

namespace Hikari.UI;

internal static class ReactComponentBuilder
{
    public static UIElement BuildUIElement(this IReactComponent component)
    {
        var source = component.GetSource();
        var obj = source.Instantiate<object>();
        while(true) {
            switch(obj) {
                case UIElement element: {
                    Debug.Assert(element.Parent == null);
                    element.ModelAlive.Subscribe(_ => component.OnMount());
                    element.ModelDead.Subscribe(_ => component.OnUnmount());
                    element.ModelEarlyUpdate.Subscribe(element =>
                    {
                        if(component.NeedsToRerender) {
                            var source = component.GetSource();
                            var target = (IReactive)element;
                            target.ApplyDiff(source);
                            component.RenderCompleted(target);
                        }
                    });
                    component.RenderCompleted(element);
                    return element;
                }
                case IReactComponent c: {
                    var element = c.BuildUIElement();
                    element.ModelAlive.Subscribe(_ => c.OnMount());
                    element.ModelDead.Subscribe(_ => c.OnUnmount());
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
#endif
