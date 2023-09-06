#nullable enable

namespace Hikari;

public interface IGBufferProvider
{
    GBuffer GetCurrentGBuffer();
    Event<IGBufferProvider> GBufferChanged { get; }
}
