#nullable enable

namespace Elffy.Bind;

internal static class Wgpu
{
    //internal sealed class BindGroupLayout : INativeTypeMarker { private BindGroupLayout() { } }
    //internal sealed class BindGroup : INativeTypeMarker { private BindGroup() { } }
    //internal sealed class Buffer : INativeTypeMarker { private Buffer() { } }
    //internal sealed class RenderPipeline : INativeTypeMarker { private RenderPipeline() { } }
    //internal sealed class Sampler : INativeTypeMarker { private Sampler() { } }
    //internal sealed class PipelineLayout : INativeTypeMarker { private PipelineLayout() { } }
    //internal sealed class ShaderModule : INativeTypeMarker { private ShaderModule() { } }
    //internal sealed class Texture : INativeTypeMarker { private Texture() { } }
    //internal sealed class TextureView : INativeTypeMarker { private TextureView() { } }
    //internal sealed class CommandEncoder : INativeTypeMarker { private CommandEncoder() { } }
    //internal sealed class RenderPass : INativeTypeMarker { private RenderPass() { } }

    internal readonly struct BindGroupLayout : INativeTypeMarker { }
    internal readonly struct BindGroup : INativeTypeMarker { }
    internal readonly struct Buffer : INativeTypeMarker { }
    internal readonly struct RenderPipeline : INativeTypeMarker { }
    internal readonly struct Sampler : INativeTypeMarker { }
    internal readonly struct PipelineLayout : INativeTypeMarker { }
    internal readonly struct ShaderModule : INativeTypeMarker { }
    internal readonly struct Texture : INativeTypeMarker { }
    internal readonly struct TextureView : INativeTypeMarker { }
    internal readonly struct CommandEncoder : INativeTypeMarker { }
    internal readonly struct RenderPass : INativeTypeMarker { }
}
