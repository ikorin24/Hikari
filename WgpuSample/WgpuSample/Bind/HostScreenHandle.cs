#nullable enable
using System;
using u8 = System.Byte;

namespace Elffy.Bind;

internal unsafe readonly record struct HostScreenHandle(NativePointer Pointer) : IHandle<HostScreenHandle>
{
    public static HostScreenHandle DestroyedHandle => default;
    public static explicit operator HostScreenHandle(void* nativePtr) => new(nativePtr);

    public (uint Width, uint Height) InnerSize
    {
        get => EngineCore.ScreenGetInnerSize(this);
        set => EngineCore.ScreenSetInnerSize(this, value.Width, value.Height);
    }

    public void WriteTexture(
            in ImageCopyTexture texture,
            Slice<u8> data,
            in wgpu_ImageDataLayout dataLayout,
            in wgpu_Extent3d size)
    {
        fixed(ImageCopyTexture* texturePtr = &texture)
        fixed(wgpu_ImageDataLayout* dataLayoutPtr = &dataLayout)
        fixed(wgpu_Extent3d* sizePtr = &size) {
            EngineCore.WriteTexture(this, texturePtr, data, dataLayoutPtr, sizePtr);
        }
    }

    public TextureHandle CreateTexture(
            in TextureDescriptor desc)
    {
        fixed(TextureDescriptor* descPtr = &desc) {
            return EngineCore.CreateTexture(this, descPtr);
        }
    }

    public BufferHandle CreateBufferInit(
            Slice<u8> contents,
            wgpu_BufferUsages usage)
    {
        return EngineCore.CreateBufferInit(this, contents, usage);
    }

    public BindGroupLayoutHandle CreateBindGroupLayout(
            in BindGroupLayoutDescriptor desc)
    {
        fixed(BindGroupLayoutDescriptor* descPtr = &desc) {
            return EngineCore.CreateBindGroupLayout(this, descPtr);
        }
    }

    public BindGroupHandle CreateBindGroup(
            in BindGroupDescriptor desc)
    {
        fixed(BindGroupDescriptor* descPtr = &desc) {
            return EngineCore.CreateBindGroup(this, descPtr);
        }
    }

    public ShaderModuleHandle CreateShaderModule(ReadOnlySpan<byte> shaderSource)
    {
        fixed(byte* ptr = shaderSource) {
            return EngineCore.CreateShaderModule(this, new Slice<u8>(ptr, shaderSource.Length));
        }
    }

    public SamplerHandle CreateSampler(in SamplerDescriptor desc)
    {
        fixed(SamplerDescriptor* descPtr = &desc) {
            return EngineCore.CreateSampler(this, descPtr);
        }
    }

    public PipelineLayoutHandle CreatePipelineLayout(in PipelineLayoutDescriptor desc)
    {
        fixed(PipelineLayoutDescriptor* descPtr = &desc) {
            return EngineCore.CreatePipelineLayout(this, descPtr);
        }
    }

    public RenderPipelineHandle CreateRenderPipeline(in RenderPipelineDescriptor desc)
    {
        fixed(RenderPipelineDescriptor* descPtr = &desc) {
            return EngineCore.CreateRenderPipeline(this, descPtr);
        }
    }
}
