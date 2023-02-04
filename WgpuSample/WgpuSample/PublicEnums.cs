#nullable enable
using Elffy.Bind;

namespace Elffy;

public enum WindowStyle : byte
{
    [EnumMapTo(CE.WindowStyle.Default)]
    Default = 0,
    [EnumMapTo(CE.WindowStyle.Fixed)]
    Fixed = 1,
    [EnumMapTo(CE.WindowStyle.Fullscreen)]
    Fullscreen = 2,
}

public enum GraphicsBackend : byte
{
    /// <summary>Empty, Don't use this value</summary>
    [EnumMapTo(Wgpu.Backends.NONE)]
    None = 0,
    /// <summary>Vulkan</summary>
    [EnumMapTo(Wgpu.Backends.VULKAN)]
    Vulkan = 1,
    /// <summary>OpenGL</summary>
    [EnumMapTo(Wgpu.Backends.GL)]
    Gl = 2,
    /// <summary>Metal</summary>
    [EnumMapTo(Wgpu.Backends.METAL)]
    Metal = 3,
    /// <summary>Direct3D 12</summary>
    [EnumMapTo(Wgpu.Backends.DX12)]
    Dx12 = 4,
    /// <summary>Direct3D 11</summary>
    [EnumMapTo(Wgpu.Backends.DX11)]
    Dx11 = 5,
    /// <summary>Browser WebGPU</summary>
    [EnumMapTo(Wgpu.Backends.BROWSER_WEBGPU)]
    BrowserWebGpu = 6,

    /// <summary>
    /// Automatically selected one of the following that is available;
    /// Vulkan, OpenGL, Metal, Direct3D 12, Direct3D 11, Browser WebGPU
    /// </summary>
    [EnumMapTo(Wgpu.Backends.VULKAN | Wgpu.Backends.GL | Wgpu.Backends.METAL | Wgpu.Backends.DX12 | Wgpu.Backends.DX11 | Wgpu.Backends.BROWSER_WEBGPU)]
    AllAvailable = 255,

    /// <summary>
    /// Automatically selected one of the following that is available;
    /// Vulkan, Metal, Direct3D 12, Browser WebGPU
    /// </summary>
    [EnumMapTo(Wgpu.Backends.VULKAN | Wgpu.Backends.METAL | Wgpu.Backends.DX12 | Wgpu.Backends.BROWSER_WEBGPU)]
    Primary = 254,

    /// <summary>
    /// Automatically selected one of the following that is available;
    /// OpenGL, Direct3D 11
    /// </summary>
    [EnumMapTo(Wgpu.Backends.GL | Wgpu.Backends.DX11)]
    Legacy = 253,
}
