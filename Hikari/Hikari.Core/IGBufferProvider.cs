#nullable enable

namespace Elffy;

public interface IGBufferProvider
{
    GBuffer CurrentGBuffer { get; }
    Event<GBuffer> GBufferChanged { get; }
}
