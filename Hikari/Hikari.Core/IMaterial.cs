#nullable enable

namespace Hikari;

public interface IMaterial
{
    Screen Screen { get; }
    Shader Shader { get; }
    void SetBindGroupsTo(in RenderPass renderPass, int passIndex, Renderer renderer);
}
