#nullable enable
using System;
using System.Runtime.InteropServices;

namespace Elffy;

public interface IVertex
{
    abstract static u32 VertexSize { get; }
    abstract static ReadOnlyMemory<VertexField> Fields { get; }
}

public record struct VertexField(u32 Offset, u32 Size, VertexFormat Format, VertexFieldSemantics Semantics);

public enum VertexFieldSemantics
{
    /// <summary>The field should be <see cref="Vector3"/> or same. (Format = <see cref="VertexFormat.Float32x3"/>)</summary>
    Position = 0,
    /// <summary>The field should be <see cref="Vector2"/>. (Format = <see cref="VertexFormat.Float32x2"/>)</summary>
    UV,
    /// <summary>The field should be <see cref="Vector3"/>. (Format = <see cref="VertexFormat.Float32x3"/>)</summary>
    Normal,
    /// <summary>The field should be <see cref="Vector4"/>. (Format = <see cref="VertexFormat.Float32x4"/>)</summary>
    Color,
    /// <summary>The field should be <see cref="uint"/>. (Format = <see cref="VertexFormat.Uint32"/>)</summary>
    TextureIndex,
    /// <summary>The field should be <see cref="Vector4u"/>. (Format = <see cref="VertexFormat.Uint32x4"/>)</summary>
    Bone,
    /// <summary>The field should be <see cref="Vector4"/>. (Format = <see cref="VertexFormat.Float32x4"/>)</summary>
    Weight,
    /// <summary>The field should be <see cref="Vector3"/>. (Format = <see cref="VertexFormat.Float32x3"/>)</summary>
    Tangent,
}

[StructLayout(LayoutKind.Explicit, Size = 32)]
public struct Vertex : IEquatable<Vertex>, IVertex
{
    [FieldOffset(0)] public Vector3 Position;
    [FieldOffset(12)] public Vector3 Normal;
    [FieldOffset(24)] public Vector2 UV;

    public static uint VertexSize => 32;

    public static ReadOnlyMemory<VertexField> Fields { get; } = new[]
    {
        new VertexField(0, 12, VertexFormat.Float32x3, VertexFieldSemantics.Position),
        new VertexField(12, 12, VertexFormat.Float32x3, VertexFieldSemantics.Normal),
        new VertexField(24, 32, VertexFormat.Float32x2, VertexFieldSemantics.UV),
    };

    public Vertex(Vector3 position, Vector3 normal, Vector2 uv)
    {
        Position = position;
        Normal = normal;
        UV = uv;
    }

    public Vertex(float px, float py, float pz, float nx, float ny, float nz, float u, float v)
    {
        Position = new Vector3(px, py, pz);
        Normal = new Vector3(nx, ny, nz);
        UV = new Vector2(u, v);
    }

    public override bool Equals(object? obj) => obj is Vertex vertex && Equals(vertex);

    public bool Equals(Vertex other)
        => Position.Equals(other.Position) &&
           Normal.Equals(other.Normal) &&
           UV.Equals(other.UV);

    public override int GetHashCode() => HashCode.Combine(Position, Normal, UV);

    public static bool operator ==(Vertex left, Vertex right) => left.Equals(right);

    public static bool operator !=(Vertex left, Vertex right) => !(left == right);
}

[StructLayout(LayoutKind.Explicit, Size = 20)]
public struct VertexSlim : IEquatable<VertexSlim>, IVertex
{
    [FieldOffset(0)] public Vector3 Position;
    [FieldOffset(12)] public Vector2 UV;

    public static uint VertexSize => 20;

    public static ReadOnlyMemory<VertexField> Fields { get; } = new[]
    {
        new VertexField(0, 12, VertexFormat.Float32x3, VertexFieldSemantics.Position),
        new VertexField(12, 8, VertexFormat.Float32x2, VertexFieldSemantics.UV),
    };

    public VertexSlim(Vector3 position, Vector2 uv)
    {
        Position = position;
        UV = uv;
    }

    public VertexSlim(float px, float py, float pz, float u, float v)
    {
        Position = new Vector3(px, py, pz);
        UV = new Vector2(u, v);
    }

    public override bool Equals(object? obj) => obj is VertexSlim slim && Equals(slim);

    public bool Equals(VertexSlim other)
        => Position.Equals(other.Position) &&
           UV.Equals(other.UV);

    public override int GetHashCode() => HashCode.Combine(Position, UV);

    public static bool operator ==(VertexSlim left, VertexSlim right) => left.Equals(right);

    public static bool operator !=(VertexSlim left, VertexSlim right) => !(left == right);
}

[StructLayout(LayoutKind.Explicit, Size = 24)]
public struct VertexPosNormal : IEquatable<VertexPosNormal>, IVertex
{
    [FieldOffset(0)] public Vector3 Position;
    [FieldOffset(12)] public Vector3 Normal;

    public static uint VertexSize => 24;

    public static ReadOnlyMemory<VertexField> Fields { get; } = new[]
    {
        new VertexField(0, 12, VertexFormat.Float32x3, VertexFieldSemantics.Position),
        new VertexField(12, 12, VertexFormat.Float32x3, VertexFieldSemantics.Normal),
    };

    public VertexPosNormal(Vector3 position, Vector3 normal)
    {
        Position = position;
        Normal = normal;
    }

    public override bool Equals(object? obj) => obj is VertexPosNormal normal && Equals(normal);

    public bool Equals(VertexPosNormal other)
        => Position.Equals(other.Position) &&
           Normal.Equals(other.Normal);

    public override int GetHashCode() => HashCode.Combine(Position, Normal);

    public static bool operator ==(VertexPosNormal left, VertexPosNormal right) => left.Equals(right);

    public static bool operator !=(VertexPosNormal left, VertexPosNormal right) => !(left == right);
}

[StructLayout(LayoutKind.Explicit, Size = 12)]
public struct VertexPosOnly : IEquatable<VertexPosOnly>, IVertex
{
    [FieldOffset(0)] public Vector3 Position;

    public VertexPosOnly(float posX, float posY, float posZ)
    {
        Position.X = posX;
        Position.Y = posY;
        Position.Z = posZ;
    }

    public static uint VertexSize => 12;

    public static ReadOnlyMemory<VertexField> Fields { get; } = new[]
    {
        new VertexField(0, 12, VertexFormat.Float32x3, VertexFieldSemantics.Position),
    };

    public override bool Equals(object? obj) => obj is VertexPosOnly only && Equals(only);

    public bool Equals(VertexPosOnly other) => Position.Equals(other.Position);

    public override int GetHashCode() => HashCode.Combine(Position);

    public static bool operator ==(VertexPosOnly left, VertexPosOnly right) => left.Equals(right);

    public static bool operator !=(VertexPosOnly left, VertexPosOnly right) => !(left == right);
}

[StructLayout(LayoutKind.Explicit, Size = 68)]
public struct SkinnedVertex : IVertex, IEquatable<SkinnedVertex>
{
    [FieldOffset(0)] public Vector3 Position;
    [FieldOffset(12)] public Vector3 Normal;
    [FieldOffset(24)] public Vector2 UV;
    [FieldOffset(32)] public Vector4u Bone;
    [FieldOffset(48)] public Vector4 Weight;
    [FieldOffset(64)] public uint TextureIndex;

    public static uint VertexSize => 68;

    public static ReadOnlyMemory<VertexField> Fields { get; } = new[]
    {
        new VertexField(0, 12, VertexFormat.Float32x3, VertexFieldSemantics.Position),
        new VertexField(12, 12, VertexFormat.Float32x3, VertexFieldSemantics.Normal),
        new VertexField(24, 8, VertexFormat.Float32x2, VertexFieldSemantics.UV),
        new VertexField(32, 16, VertexFormat.Uint32x4, VertexFieldSemantics.Bone),
        new VertexField(48, 16, VertexFormat.Float32x4, VertexFieldSemantics.Weight),
        new VertexField(64, 4, VertexFormat.Uint32, VertexFieldSemantics.TextureIndex),
    };

    public override bool Equals(object? obj)
    {
        return obj is SkinnedVertex vertex && Equals(vertex);
    }

    public bool Equals(SkinnedVertex other)
    {
        return Position.Equals(other.Position) &&
               Normal.Equals(other.Normal) &&
               UV.Equals(other.UV) &&
               Bone.Equals(other.Bone) &&
               Weight.Equals(other.Weight) &&
               TextureIndex == other.TextureIndex;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Position, Normal, UV, Bone, Weight, TextureIndex);
    }

    public static bool operator ==(SkinnedVertex left, SkinnedVertex right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SkinnedVertex left, SkinnedVertex right)
    {
        return !(left == right);
    }
}
