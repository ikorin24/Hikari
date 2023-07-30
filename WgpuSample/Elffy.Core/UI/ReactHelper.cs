#nullable enable

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
            if(source.HasObjectType(out var objectType)) {
                if(objectType.IsAssignableTo(reactive?.GetType()) == true) {
                    ((IReactive)reactive).ApplyDiff(source);
                    isNew = false;
                    return reactive;
                }
            }
        }

        isNew = true;
        return source.Instantiate<T>();
    }
}
