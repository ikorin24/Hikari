#if HIKARI_JSON_SERDE
#nullable enable

namespace Hikari.UI;

public interface IReactComponent : IReactive
{
    bool NeedsToRerender { get; }
    ObjectSource GetSource();
    void RenderCompleted<T>(T rendered);
    void OnMount();
    void OnUnmount();
}
#endif
