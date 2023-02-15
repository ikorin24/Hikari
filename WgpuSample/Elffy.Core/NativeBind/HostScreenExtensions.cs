#nullable enable
using System;
using Self = Elffy.NativeBind.CoreElffy.HostScreen;

namespace Elffy.NativeBind;

internal unsafe static class HostScreenExtensions
{
    public static void WriteTexture(
        this Rust.Ref<Self> self,
        in CE.ImageCopyTexture texture,
        CE.Slice<u8> data,
        in Wgpu.ImageDataLayout dataLayout,
        in Wgpu.Extent3d size)
    {
        fixed(CE.ImageCopyTexture* texturePtr = &texture)
        fixed(Wgpu.ImageDataLayout* dataLayoutPtr = &dataLayout)
        fixed(Wgpu.Extent3d* sizePtr = &size) {
            EngineCore.WriteTexture(self, texturePtr, data, dataLayoutPtr, sizePtr);
        }
    }

    public static Rust.Box<Wgpu.Texture> CreateTexture(
        this Rust.Ref<Self> self,
        in CE.TextureDescriptor desc)
    {
        fixed(CE.TextureDescriptor* descPtr = &desc) {
            return EngineCore.CreateTexture(self, descPtr);
        }
    }

    public static Rust.Box<Wgpu.Buffer> CreateBufferInit(
        this Rust.Ref<Self> self,
        ReadOnlySpan<u8> contents,
        Wgpu.BufferUsages usage)
    {
        fixed(u8* p = contents) {
            var slice = new CE.Slice<byte>(p, contents.Length);
            return EngineCore.CreateBufferInit(self, slice, usage);
        }
    }

    public static Rust.Box<Wgpu.BindGroupLayout> CreateBindGroupLayout(
        this Rust.Ref<Self> self,
        in CE.BindGroupLayoutDescriptor desc)
    {
        fixed(CE.BindGroupLayoutDescriptor* descPtr = &desc) {
            return EngineCore.CreateBindGroupLayout(self, descPtr);
        }
    }

    public static Rust.Box<Wgpu.BindGroup> CreateBindGroup(
        this Rust.Ref<Self> self,
        in CE.BindGroupDescriptor desc)
    {
        fixed(CE.BindGroupDescriptor* descPtr = &desc) {
            return EngineCore.CreateBindGroup(self, descPtr);
        }
    }

    public static Rust.Box<Wgpu.ShaderModule> CreateShaderModule(
        this Rust.Ref<Self> self,
        ReadOnlySpan<byte> shaderSource)
    {
        fixed(byte* ptr = shaderSource) {
            return EngineCore.CreateShaderModule(self, new CE.Slice<u8>(ptr, shaderSource.Length));
        }
    }

    public static Rust.Box<Wgpu.Sampler> CreateSampler(
        this Rust.Ref<Self> self,
        in CE.SamplerDescriptor desc)
    {
        fixed(CE.SamplerDescriptor* descPtr = &desc) {
            return EngineCore.CreateSampler(self, descPtr);
        }
    }

    public static Rust.Box<Wgpu.PipelineLayout> CreatePipelineLayout(
        this Rust.Ref<Self> self,
        in CE.PipelineLayoutDescriptor desc)
    {
        fixed(CE.PipelineLayoutDescriptor* descPtr = &desc) {
            return EngineCore.CreatePipelineLayout(self, descPtr);
        }
    }

    public static Rust.Box<Wgpu.RenderPipeline> CreateRenderPipeline(
        this Rust.Ref<Self> self,
        in CE.RenderPipelineDescriptor desc)
    {
        fixed(CE.RenderPipelineDescriptor* descPtr = &desc) {
            return EngineCore.CreateRenderPipeline(self, descPtr);
        }
    }
}


internal unsafe static class TextureExtensions
{
    public static Rust.Box<Wgpu.TextureView> CreateTextureView(this Rust.Ref<Wgpu.Texture> self)
        => CreateTextureView(self, CE.TextureViewDescriptor.Default);

    public static Rust.Box<Wgpu.TextureView> CreateTextureView(
        this Rust.Ref<Wgpu.Texture> self,
        in CE.TextureViewDescriptor desc)
    {
        fixed(CE.TextureViewDescriptor* descPtr = &desc) {
            return EngineCore.CreateTextureView(self, descPtr);
        }
    }
}

internal unsafe static class BufferExtensions
{
    public static CE.BufferBinding AsEntireBufferBinding(
        this Rust.Ref<Wgpu.Buffer> self)
    {
        return new CE.BufferBinding
        {
            buffer = self,
            offset = 0,
            size = 0,
        };
    }

    public static CE.BufferSlice AsSlice(this Rust.Ref<Wgpu.Buffer> self) => new CE.BufferSlice(self, CE.RangeBoundsU64.RangeFull);

    public static CE.BufferSlice AsSlice(this Rust.Ref<Wgpu.Buffer> self, CE.RangeBoundsU64 range) => new CE.BufferSlice(self, range);
}
