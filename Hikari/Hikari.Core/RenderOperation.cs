#nullable enable

namespace Hikari;

public abstract class RenderOperation<TSelf, TShader, TMaterial>
    : Operation<TSelf>
    where TSelf : RenderOperation<TSelf, TShader, TMaterial>
    where TShader : Shader<TShader, TMaterial, TSelf>
    where TMaterial : Material<TMaterial, TShader, TSelf>
{
    private readonly Own<PipelineLayout> _pipelineLayout;

    public PipelineLayout PipelineLayout => _pipelineLayout.AsValue();

    /// <summary>Strong typed `this`</summary>
    protected TSelf This => SafeCast.As<TSelf>(this);

    private protected RenderOperation(Screen screen, Own<PipelineLayout> pipelineLayout)
        : base(screen)
    {
        // `this` must be of type `TSelf`.
        // This is true as long as a derived class is implemented correctly.
        if(this is not TSelf) {
            ThrowHelper.ThrowInvalidOperation("Invalid self type.");
        }

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
