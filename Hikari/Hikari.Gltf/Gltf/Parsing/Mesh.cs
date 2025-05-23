﻿#nullable enable
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Hikari.Gltf.Parsing;

internal struct Mesh
{
    private static readonly MeshPrimitive[] _emptyPrimitives = new MeshPrimitive[0];

    public MeshPrimitive[] primitives = _emptyPrimitives;    // must
    public float[]? weights = null;
    public U8String? name = null;

    public Mesh()
    {
    }
}

internal struct MeshPrimitive
{
    public MeshPrimitiveAttributes attributes = new MeshPrimitiveAttributes();
    public uint? indices = null;
    public uint? material = null;
    public MeshPrimitiveMode mode = MeshPrimitiveMode.Triangles;
    public MeshPrimitveTarget[]? targets = null;

    public MeshPrimitive()
    {
    }
}

internal struct MeshPrimitiveAttributes
{
    // The correct form is 'Dictionary<string, uint>'

    public uint? POSITION = null;
    public uint? COLOR_0 = null;
    public uint? JOINTS_0 = null;
    public uint? NORMAL = null;
    public uint? TANGENT = null;
    public uint? TEXCOORD_0 = null;
    public uint? TEXCOORD_1 = null;
    public uint? TEXCOORD_2 = null;
    public uint? TEXCOORD_3 = null;
    public uint? WEIGHTS_0 = null;

    public uint? this[string key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if(key == nameof(POSITION)) { return POSITION; }
            if(key == nameof(COLOR_0)) { return COLOR_0; }
            if(key == nameof(JOINTS_0)) { return JOINTS_0; }
            if(key == nameof(NORMAL)) { return NORMAL; }
            if(key == nameof(TANGENT)) { return TANGENT; }
            if(key == nameof(TEXCOORD_0)) { return TEXCOORD_0; }
            if(key == nameof(TEXCOORD_1)) { return TEXCOORD_1; }
            if(key == nameof(TEXCOORD_2)) { return TEXCOORD_2; }
            if(key == nameof(TEXCOORD_3)) { return TEXCOORD_3; }
            if(key == nameof(WEIGHTS_0)) { return WEIGHTS_0; }
            return null;
        }
    }

    public MeshPrimitiveAttributes()
    {
    }

    // TODO: avoid yield return
    public IEnumerable<(string Key, uint Value)> GetKeyValues()
    {
        if(POSITION != null) { yield return (nameof(POSITION), POSITION.Value); }
        if(COLOR_0 != null) { yield return (nameof(COLOR_0), COLOR_0.Value); }
        if(JOINTS_0 != null) { yield return (nameof(JOINTS_0), JOINTS_0.Value); }
        if(NORMAL != null) { yield return (nameof(NORMAL), NORMAL.Value); }
        if(TANGENT != null) { yield return (nameof(TANGENT), TANGENT.Value); }
        if(TEXCOORD_0 != null) { yield return (nameof(TEXCOORD_0), TEXCOORD_0.Value); }
        if(TEXCOORD_1 != null) { yield return (nameof(TEXCOORD_1), TEXCOORD_1.Value); }
        if(TEXCOORD_2 != null) { yield return (nameof(TEXCOORD_2), TEXCOORD_2.Value); }
        if(TEXCOORD_3 != null) { yield return (nameof(TEXCOORD_3), TEXCOORD_3.Value); }
        if(WEIGHTS_0 != null) { yield return (nameof(WEIGHTS_0), WEIGHTS_0.Value); }

    }
}

internal enum MeshPrimitiveMode
{
    Points = 0,
    Lines = 1,
    LineLoop = 2,
    LineStrip = 3,
    Triangles = 4,
    TriangleStrip = 5,
    TriangleFan = 6,
}

internal struct MeshPrimitveTarget
{
    // The correct form is 'Dictionary<string, uint>'

    public uint? POSITION = null;
    public uint? NORMAL = null;
    public MeshPrimitveTarget()
    {
    }
}
