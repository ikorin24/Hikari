#nullable enable
using System;

namespace Hikari;

public interface IGBufferProvider
{
    BindGroupLayout BindGroupLayout { get; }
    GBuffer GetCurrentGBuffer();
    Event<IGBufferProvider> GBufferChanged { get; }
}

public static class GBufferProviderExtensions
{
    public static EventSubscription<IGBufferProvider> Observe(this IGBufferProvider self, Action<GBuffer> observer)
    {
        observer.Invoke(self.GetCurrentGBuffer());
        return self.GBufferChanged.Subscribe(self => observer.Invoke(self.GetCurrentGBuffer()));
    }
}
