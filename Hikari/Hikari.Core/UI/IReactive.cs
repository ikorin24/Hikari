#nullable enable

namespace Elffy.UI;

public interface IReactive
{
    void ApplyDiff(in ObjectSource source);
}
