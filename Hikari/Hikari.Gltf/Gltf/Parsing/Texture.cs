﻿#nullable enable

namespace Hikari.Gltf.Parsing;

internal struct Texture
{
    public uint? sampler = null;
    public uint? source = null;
    public U8String? name = null;
    public Texture()
    {
    }
}
