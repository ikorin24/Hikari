#nullable enable
using System;
using u8 = System.Byte;
using Self = Elffy.Bind.Elffycore.HostScreen;

namespace Elffy.Bind;

internal unsafe static class HostScreenExtensions
{
    public static (uint Width, uint Height) GetInnerSize(this Ref<Self> self)
    {
        return EngineCore.ScreenGetInnerSize(self);
    }

    public static void SetInnerSize(this Ref<Self> self, uint width, uint height)
    {
        EngineCore.ScreenSetInnerSize(self, width, height);
    }

    public static void WriteTexture(
        this Ref<Self> self,
        in ImageCopyTexture texture,
        Slice<u8> data,
        in Wgpu.ImageDataLayout dataLayout,
        in Wgpu.Extent3d size)
    {
        fixed(ImageCopyTexture* texturePtr = &texture)
        fixed(Wgpu.ImageDataLayout* dataLayoutPtr = &dataLayout)
        fixed(Wgpu.Extent3d* sizePtr = &size) {
            EngineCore.WriteTexture(self, texturePtr, data, dataLayoutPtr, sizePtr);
        }
    }

    public static Box<Wgpu.Texture> CreateTexture(
        this Ref<Self> self,
        in TextureDescriptor desc)
    {
        fixed(TextureDescriptor* descPtr = &desc) {
            return EngineCore.CreateTexture(self, descPtr);
        }
    }

    public static Box<Wgpu.Buffer> CreateBufferInit(
        this Ref<Self> self,
        Slice<u8> contents,
        Wgpu.BufferUsages usage)
    {
        return EngineCore.CreateBufferInit(self, contents, usage);
    }

    public static Box<Wgpu.BindGroupLayout> CreateBindGroupLayout(
        this Ref<Self> self,
        in BindGroupLayoutDescriptor desc)
    {
        fixed(BindGroupLayoutDescriptor* descPtr = &desc) {
            return EngineCore.CreateBindGroupLayout(self, descPtr);
        }
    }

    public static Box<Wgpu.BindGroup> CreateBindGroup(
        this Ref<Self> self,
        in BindGroupDescriptor desc)
    {
        fixed(BindGroupDescriptor* descPtr = &desc) {
            return EngineCore.CreateBindGroup(self, descPtr);
        }
    }

    public static Box<Wgpu.ShaderModule> CreateShaderModule(
        this Ref<Self> self,
        ReadOnlySpan<byte> shaderSource)
    {
        fixed(byte* ptr = shaderSource) {
            return EngineCore.CreateShaderModule(self, new Slice<u8>(ptr, shaderSource.Length));
        }
    }

    public static Box<Wgpu.Sampler> CreateSampler(
        this Ref<Self> self,
        in SamplerDescriptor desc)
    {
        fixed(SamplerDescriptor* descPtr = &desc) {
            return EngineCore.CreateSampler(self, descPtr);
        }
    }

    public static Box<Wgpu.PipelineLayout> CreatePipelineLayout(
        this Ref<Self> self,
        in PipelineLayoutDescriptor desc)
    {
        fixed(PipelineLayoutDescriptor* descPtr = &desc) {
            return EngineCore.CreatePipelineLayout(self, descPtr);
        }
    }

    public static Box<Wgpu.RenderPipeline> CreateRenderPipeline(
        this Ref<Self> self,
        in RenderPipelineDescriptor desc)
    {
        fixed(RenderPipelineDescriptor* descPtr = &desc) {
            return EngineCore.CreateRenderPipeline(self, descPtr);
        }
    }
}


internal unsafe static class TextureExtensions
{
    public static Box<Wgpu.TextureView> CreateTextureView(this Ref<Wgpu.Texture> self)
        => CreateTextureView(self, TextureViewDescriptor.Default);

    public static Box<Wgpu.TextureView> CreateTextureView(
        this Ref<Wgpu.Texture> self,
        in TextureViewDescriptor desc)
    {
        fixed(TextureViewDescriptor* descPtr = &desc) {
            return EngineCore.CreateTextureView(self, descPtr);
        }
    }
}

internal unsafe static class BufferExtensions
{
    public static BufferBinding AsEntireBufferBinding(
        this Ref<Wgpu.Buffer> self)
    {
        return new BufferBinding
        {
            buffer = self,
            offset = 0,
            size = 0,
        };
    }

    public static BufferSlice AsSlice(this Ref<Wgpu.Buffer> self) => new BufferSlice(self, RangeBoundsU64.RangeFull);

    public static BufferSlice AsSlice(this Ref<Wgpu.Buffer> self, RangeBoundsU64 range) => new BufferSlice(self, range);
}
