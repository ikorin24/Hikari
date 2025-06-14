﻿#nullable enable
using Hikari.NativeBind;

namespace Hikari;

public enum WindowStyle : byte
{
    [EnumMapTo(CH.WindowStyle.Default)]
    Default = 0,
    [EnumMapTo(CH.WindowStyle.Fixed)]
    Fixed = 1,
    [EnumMapTo(CH.WindowStyle.Fullscreen)]
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
    GL = 2,
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
}

public enum SurfacePresentMode
{
    [EnumMapTo(CH.PresentMode.Fifo)]
    VsyncOn = 0,
    [EnumMapTo(CH.PresentMode.Immediate)]
    VsyncOff = 1,
    [EnumMapTo(CH.PresentMode.FifoRelaxed)]
    AdaptiveVsync = 2,
    [EnumMapTo(CH.PresentMode.Mailbox)]
    FastVsync = 3,
}
