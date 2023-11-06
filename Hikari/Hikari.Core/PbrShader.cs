#nullable enable

namespace Hikari;

public abstract class PbrShader : Shader<PbrShader, PbrMaterial>
{
    protected PbrShader(Screen screen, in ShaderPassDescriptorArray1 passes) : base(screen, passes)
    {
    }

    protected PbrShader(Screen screen, in ShaderPassDescriptorArray2 passes) : base(screen, passes)
    {
    }

    protected PbrShader(Screen screen, in ShaderPassDescriptorArray3 passes) : base(screen, passes)
    {
    }

    protected PbrShader(Screen screen, in ShaderPassDescriptorArray4 passes) : base(screen, passes)
    {
    }
}
