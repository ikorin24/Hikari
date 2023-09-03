#nullable enable

namespace Hikari;

public interface IGBufferProvider
{
    GBuffer GetCurrentGBuffer();
    Event<GBuffer> GBufferChanged { get; }
}
