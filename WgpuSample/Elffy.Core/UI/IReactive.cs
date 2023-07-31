#nullable enable

namespace Elffy.UI;

public interface IReactive
{
    void ApplyDiff(in ReactSource source);
}
