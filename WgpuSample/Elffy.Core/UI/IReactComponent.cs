#nullable enable

namespace Elffy.UI;

public interface IReactComponent : IReactive
{
    bool NeedsToRerender { get; }
    ReactSource GetReactSource();
    void RenderCompleted<T>(T rendered);
    void OnMount();
    void OnUnmount();
}
