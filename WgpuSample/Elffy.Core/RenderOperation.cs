#nullable enable
using System;

namespace Elffy;

public sealed class RenderOperation
{
    private readonly IHostScreen _screen;
    private readonly Shader _shader;
    private readonly Own<RenderPipeline> _pipeline;

    public Shader Shader => _shader;
    public RenderPipeline Pipeline => _pipeline.AsValue();

    public RenderOperation(Shader shader, in RenderPipelineDescriptor pipelineDesc)
    {
        ArgumentNullException.ThrowIfNull(shader);
        _screen = shader.Screen;
        _shader = shader;
        _pipeline = RenderPipeline.Create(_screen, in pipelineDesc);
    }

    private void Release()
    {
        _pipeline.Dispose();
    }

    public static Own<RenderOperation> Create(Shader shader, in RenderPipelineDescriptor pipelineDesc)
    {
        return new(new RenderOperation(shader, pipelineDesc), static self => self.Release());
    }
}
