#nullable enable

using System;

namespace Elffy.UI;

internal static class ReactHelper
{
    public static T ApplyDiffOrNew<T>(T? reactive, ReactSource source)
    {
        return ApplyDiffOrNew(reactive, source, out _);
    }

    public static T ApplyDiffOrNew<T>(T? reactive, ReactSource source, out bool isNew)
    {
        if(typeof(T).IsAssignableTo(typeof(IReactive))) {

            if(source.IsArray) {

            }

            var objectType = source.ObjectType;
            if(objectType?.IsAssignableTo(reactive?.GetType()) == true) {
                ((IReactive)reactive).ApplyDiff(source);
                isNew = false;
                return reactive;
            }
        }

        isNew = true;
        return source.Instantiate<T>();
    }

    //public static T ApplyDiffOrNew<T>(T? reactive, ReactSource source, out bool isNew) where T : IReactive
    //{
    //    var objectType = source.ObjectType;
    //    if(objectType?.IsAssignableTo(reactive?.GetType()) == true) {
    //        reactive.ApplyDiff(source);
    //        isNew = false;
    //        return reactive;
    //    }
    //    isNew = true;
    //    return source.CreateNew<T>();
    //}

    //public static object Apply(IReactComponent component)
    //{
    //    var source = component.GetReactSource();
    //    return Apply(component, source);
    //}
}
