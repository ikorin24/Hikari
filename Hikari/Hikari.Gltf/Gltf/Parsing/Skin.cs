#nullable enable
using System;

namespace Hikari.Gltf.Parsing;

internal struct Skin
{
    public uint? inverseBindMatrices = null;
    public uint? skeleton = null;
    public uint[] joints = Array.Empty<uint>();  // must
    public U8String? name = null;

    public Skin()
    {
    }
}
