#nullable enable
using System;

namespace Elffy;

public sealed class RenderOperation
{
    private readonly IHostScreen _screen;
    private readonly Own<RenderPipeline> _pipelineOwn;
    private readonly Own<Shader> _shaderOwn;

    public IHostScreen Screen => _screen;
    public Shader Shader => _shaderOwn.AsValue();
    public RenderPipeline Pipeline => _pipelineOwn.AsValue();

    private RenderOperation(IHostScreen screen, Own<Shader> shaderOwn, Own<RenderPipeline> pipelineOwn)
    {
        _screen = screen;
        _shaderOwn = shaderOwn;
        _pipelineOwn = pipelineOwn;
    }

    private void Release()
    {
        _pipelineOwn.Dispose();
    }

    public static Own<RenderOperation> Create(Own<Shader> shader, in RenderPipelineDescriptor pipelineDesc)
    {
        shader.ThrowArgumentExceptionIfNone();
        var screen = shader.AsValue().Screen;
        var pipeline = RenderPipeline.Create(screen, in pipelineDesc);
        var self = new RenderOperation(screen, shader, pipeline);
        return new(self, static self => self.Release());
    }
}
