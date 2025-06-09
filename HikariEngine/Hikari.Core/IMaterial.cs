#nullable enable

namespace Hikari;

public interface IMaterial
{
    Screen Screen { get; }
    ITypedShader Shader { get; }
    void SetBindGroupsTo(in RenderPass renderPass, int passIndex, Renderer renderer);
}
