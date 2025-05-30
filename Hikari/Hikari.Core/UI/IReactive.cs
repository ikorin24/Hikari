#if HIKARI_JSON_SERDE
#nullable enable

namespace Hikari.UI;

public interface IReactive
{
    void ApplyDiff(in ObjectSource source);
}
#endif
