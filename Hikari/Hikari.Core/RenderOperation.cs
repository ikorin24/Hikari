#nullable enable

namespace Elffy;

public abstract class RenderOperation<TSelf, TShader, TMaterial>
    : Operation
    where TSelf : RenderOperation<TSelf, TShader, TMaterial>
    where TShader : Shader<TShader, TMaterial, TSelf>
    where TMaterial : Material<TMaterial, TShader, TSelf>
{
    private readonly Own<PipelineLayout> _pipelineLayout;

    public PipelineLayout PipelineLayout => _pipelineLayout.AsValue();

    protected RenderOperation(Screen screen, Own<PipelineLayout> pipelineLayout, int sortOrder)
        : base(screen, sortOrder)
    {
        pipelineLayout.ThrowArgumentExceptionIfNone();
        _pipelineLayout = pipelineLayout;
    }

    protected sealed override void Execute(in OperationContext context)
    {
        using var pass = CreateRenderPass(in context);
        Render(pass.AsValue());
    }

    protected abstract OwnRenderPass CreateRenderPass(in OperationContext context);

    protected abstract void Render(in RenderPass pass);
}
