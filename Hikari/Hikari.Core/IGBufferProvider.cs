#nullable enable

namespace Hikari;

public interface IGBufferProvider
{
    GBuffer CurrentGBuffer { get; }
    Event<GBuffer> GBufferChanged { get; }
}
