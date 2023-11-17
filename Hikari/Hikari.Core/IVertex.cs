#nullable enable
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikari;

public interface IVertex : IVertexPosition
{
    abstract static u32 VertexSize { get; }
    abstract static VertexFields Fields { get; }
}

public static class VertexAccessor
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly TField GetField<TVertex, TField>(in TVertex v, uint offset)
        where TVertex : unmanaged
        where TField : unmanaged
    {
        return ref Unsafe.As<TVertex, TField>(
            ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in v), offset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TField GetRefField<TVertex, TField>(ref TVertex v, uint offset)
        where TVertex : unmanaged
        where TField : unmanaged
    {
        return ref Unsafe.As<TVertex, TField>(
            ref Unsafe.AddByteOffset(ref v, offset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly Vector3 Position<TVertex>(in TVertex v)
        where TVertex : unmanaged, IVertex, IVertexPosition
    {
        return ref Unsafe.As<TVertex, Vector3>(
            ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in v), TVertex.PositionOffset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref Vector3 RefPosition<TVertex>(ref TVertex v)
        where TVertex : unmanaged, IVertex, IVertexPosition
    {
        return ref Unsafe.As<TVertex, Vector3>(
            ref Unsafe.AddByteOffset(ref v, TVertex.PositionOffset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly Vector2 UV<TVertex>(in TVertex v)
    where TVertex : unmanaged, IVertex, IVertexUV
    {
        return ref Unsafe.As<TVertex, Vector2>(
            ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in v), TVertex.UVOffset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref Vector2 RefUV<TVertex>(ref TVertex v)
        where TVertex : unmanaged, IVertex, IVertexUV
    {
        return ref Unsafe.As<TVertex, Vector2>(
            ref Unsafe.AddByteOffset(ref v, TVertex.UVOffset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly Vector3 Normal<TVertex>(in TVertex v)
        where TVertex : unmanaged, IVertex, IVertexNormal
    {
        return ref Unsafe.As<TVertex, Vector3>(
            ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in v), TVertex.NormalOffset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref Vector3 RefNormal<TVertex>(ref TVertex v)
        where TVertex : unmanaged, IVertex, IVertexNormal
    {
        return ref Unsafe.As<TVertex, Vector3>(
            ref Unsafe.AddByteOffset(ref v, TVertex.NormalOffset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly Vector4 Color<TVertex>(in TVertex v)
        where TVertex : unmanaged, IVertex, IVertexColor
    {
        return ref Unsafe.As<TVertex, Vector4>(
            ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in v), TVertex.ColorOffset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref Vector4 RefColor<TVertex>(ref TVertex v)
        where TVertex : unmanaged, IVertex, IVertexColor
    {
        return ref Unsafe.As<TVertex, Vector4>(
            ref Unsafe.AddByteOffset(ref v, TVertex.ColorOffset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly uint TextureIndex<TVertex>(in TVertex v)
        where TVertex : unmanaged, IVertex, IVertexTextureIndex
    {
        return ref Unsafe.As<TVertex, uint>(
            ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in v), TVertex.TextureIndexOffset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref uint RefTextureIndex<TVertex>(ref TVertex v)
        where TVertex : unmanaged, IVertex, IVertexTextureIndex
    {
        return ref Unsafe.As<TVertex, uint>(
            ref Unsafe.AddByteOffset(ref v, TVertex.TextureIndexOffset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly Vector4u Bone<TVertex>(in TVertex v)
        where TVertex : unmanaged, IVertex, IVertexBone
    {
        return ref Unsafe.As<TVertex, Vector4u>(
            ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in v), TVertex.BoneOffset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref Vector4u RefBone<TVertex>(ref TVertex v)
        where TVertex : unmanaged, IVertex, IVertexBone
    {
        return ref Unsafe.As<TVertex, Vector4u>(
            ref Unsafe.AddByteOffset(ref v, TVertex.BoneOffset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly Vector4 Weight<TVertex>(in TVertex v)
        where TVertex : unmanaged, IVertex, IVertexWeight
    {
        return ref Unsafe.As<TVertex, Vector4>(
            ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in v), TVertex.WeightOffset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref Vector4 RefWeight<TVertex>(ref TVertex v)
        where TVertex : unmanaged, IVertex, IVertexWeight
    {
        return ref Unsafe.As<TVertex, Vector4>(
            ref Unsafe.AddByteOffset(ref v, TVertex.WeightOffset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly Vector3 Tangent<TVertex>(in TVertex v)
        where TVertex : unmanaged, IVertex, IVertexTangent
    {
        return ref Unsafe.As<TVertex, Vector3>(
            ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in v), TVertex.TangentOffset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref Vector3 RefTangent<TVertex>(ref TVertex v)
        where TVertex : unmanaged, IVertex, IVertexTangent
    {
        return ref Unsafe.As<TVertex, Vector3>(
            ref Unsafe.AddByteOffset(ref v, TVertex.TangentOffset));
    }
}

public record struct VertexField(u32 Offset, u32 Size, VertexFormat Format, VertexFieldSemantics Semantics);

public interface IVertexPosition
{
    abstract static uint PositionOffset { get; }
}
public interface IVertexUV
{
    abstract static uint UVOffset { get; }
}
public interface IVertexNormal
{
    abstract static uint NormalOffset { get; }
}
public interface IVertexColor
{
    abstract static uint ColorOffset { get; }
}
public interface IVertexTextureIndex
{
    abstract static uint TextureIndexOffset { get; }
}
public interface IVertexBone
{
    abstract static uint BoneOffset { get; }
}
public interface IVertexWeight
{
    abstract static uint WeightOffset { get; }
}
public interface IVertexTangent
{
    abstract static uint TangentOffset { get; }
}

public sealed class VertexFields
{
    private readonly ImmutableArray<VertexField> _fields;

    public VertexFields(ReadOnlySpan<VertexField> fields)
    {
        Validate(fields);
        _fields = fields.ToImmutableArray();
    }

    private static void Validate(ReadOnlySpan<VertexField> fields)
    {
        for(int i = 0; i < fields.Length; i++) {
            var (validFormat, validSize) = fields[i].Semantics switch
            {
                VertexFieldSemantics.Position => (VertexFormat.Float32x3, 12u),
                VertexFieldSemantics.UV => (VertexFormat.Float32x2, 8u),
                VertexFieldSemantics.Normal => (VertexFormat.Float32x3, 12u),
                VertexFieldSemantics.Color => (VertexFormat.Float32x4, 16u),
                VertexFieldSemantics.TextureIndex => (VertexFormat.Uint32, 4u),
                VertexFieldSemantics.Bone => (VertexFormat.Uint32x4, 16u),
                VertexFieldSemantics.Weight => (VertexFormat.Float32x4, 16u),
                VertexFieldSemantics.Tangent => (VertexFormat.Float32x3, 12u),
                _ => throw new ArgumentException($"field[{i}] has invalid semantics"),
            };
            if(fields[i].Format != validFormat) {
                throw new ArgumentException($"field[{i}] has invalid format. Format of '{fields[i].Semantics}' field should be '{validFormat}'.");
            }
            if(fields[i].Size != validSize) {
                throw new ArgumentException($"field[{i}] has invalid size. Size of '{fields[i].Semantics}' field should be '{validSize}'.");
            }
        }
    }

    public bool TryGetField(VertexFieldSemantics semantics, out VertexField f)
    {
        foreach(var field in _fields) {
            if(field.Semantics == semantics) {
                f = field;
                return true;
            }
        }
        f = default;
        return false;
    }

    public VertexField GetField(VertexFieldSemantics semantics)
    {
        if(TryGetField(semantics, out var field) == false) {
            ThrowNotFound(semantics);

            [DoesNotReturn]
            static void ThrowNotFound(VertexFieldSemantics semantics) => throw new ArgumentException($"vertex field not found: {semantics}");
        }
        return field;
    }

    public bool Contains(VertexFieldSemantics semantics)
    {
        return TryGetField(semantics, out _);
    }

    public ImmutableArray<VertexField> AsImmutableArray() => _fields;
    public ReadOnlySpan<VertexField> AsSpan() => _fields.AsSpan();
}

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
public struct Vertex : IEquatable<Vertex>, IVertex, IVertexPosition, IVertexNormal, IVertexUV
{
    [FieldOffset(0)] public Vector3 Position;
    [FieldOffset(12)] public Vector3 Normal;
    [FieldOffset(24)] public Vector2 UV;

    public static uint VertexSize => 32;

    public static VertexFields Fields { get; } = new VertexFields(
    [
        new VertexField(0, 12, VertexFormat.Float32x3, VertexFieldSemantics.Position),
        new VertexField(12, 12, VertexFormat.Float32x3, VertexFieldSemantics.Normal),
        new VertexField(24, 8, VertexFormat.Float32x2, VertexFieldSemantics.UV),
    ]);

    public static uint PositionOffset => 0;

    public static uint NormalOffset => 12;

    public static uint UVOffset => 24;

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
public struct VertexSlim
    : IEquatable<VertexSlim>,
    IVertex,
    IVertexPosition, IVertexUV
{
    [FieldOffset(0)] public Vector3 Position;
    [FieldOffset(12)] public Vector2 UV;

    public static uint VertexSize => 20;

    public static VertexFields Fields { get; } = new VertexFields(
    [
        new VertexField(0, 12, VertexFormat.Float32x3, VertexFieldSemantics.Position),
        new VertexField(12, 8, VertexFormat.Float32x2, VertexFieldSemantics.UV),
    ]);

    public static uint PositionOffset => 0;

    public static uint UVOffset => 12;

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
public struct VertexPosNormal
    : IEquatable<VertexPosNormal>,
    IVertex,
    IVertexPosition,
    IVertexNormal
{
    [FieldOffset(0)] public Vector3 Position;
    [FieldOffset(12)] public Vector3 Normal;

    public static uint VertexSize => 24;

    public static VertexFields Fields { get; } = new VertexFields(
    [
        new VertexField(0, 12, VertexFormat.Float32x3, VertexFieldSemantics.Position),
        new VertexField(12, 12, VertexFormat.Float32x3, VertexFieldSemantics.Normal),
    ]);

    public static uint PositionOffset => 0;

    public static uint NormalOffset => 12;

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
public struct VertexPosOnly
    : IEquatable<VertexPosOnly>,
    IVertex,
    IVertexPosition
{
    [FieldOffset(0)] public Vector3 Position;

    public VertexPosOnly(float posX, float posY, float posZ)
    {
        Position.X = posX;
        Position.Y = posY;
        Position.Z = posZ;
    }

    public static uint VertexSize => 12;

    public static VertexFields Fields { get; } = new VertexFields(
    [
        new VertexField(0, 12, VertexFormat.Float32x3, VertexFieldSemantics.Position),
    ]);

    public static uint PositionOffset => 0;

    public override bool Equals(object? obj) => obj is VertexPosOnly only && Equals(only);

    public bool Equals(VertexPosOnly other) => Position.Equals(other.Position);

    public override int GetHashCode() => HashCode.Combine(Position);

    public static bool operator ==(VertexPosOnly left, VertexPosOnly right) => left.Equals(right);

    public static bool operator !=(VertexPosOnly left, VertexPosOnly right) => !(left == right);
}

[StructLayout(LayoutKind.Explicit, Size = 68)]
public struct SkinnedVertex
    : IEquatable<SkinnedVertex>,
    IVertex,
    IVertexPosition,
    IVertexNormal,
    IVertexUV,
    IVertexBone,
    IVertexWeight,
    IVertexTextureIndex
{
    [FieldOffset(0)] public Vector3 Position;
    [FieldOffset(12)] public Vector3 Normal;
    [FieldOffset(24)] public Vector2 UV;
    [FieldOffset(32)] public Vector4u Bone;
    [FieldOffset(48)] public Vector4 Weight;
    [FieldOffset(64)] public uint TextureIndex;

    public static uint VertexSize => 68;

    public static VertexFields Fields { get; } = new VertexFields(
    [
        new VertexField(0, 12, VertexFormat.Float32x3, VertexFieldSemantics.Position),
        new VertexField(12, 12, VertexFormat.Float32x3, VertexFieldSemantics.Normal),
        new VertexField(24, 8, VertexFormat.Float32x2, VertexFieldSemantics.UV),
        new VertexField(32, 16, VertexFormat.Uint32x4, VertexFieldSemantics.Bone),
        new VertexField(48, 16, VertexFormat.Float32x4, VertexFieldSemantics.Weight),
        new VertexField(64, 4, VertexFormat.Uint32, VertexFieldSemantics.TextureIndex),
    ]);

    public static uint PositionOffset => 0;
    public static uint NormalOffset => 12;
    public static uint UVOffset => 24;
    public static uint BoneOffset => 32;
    public static uint WeightOffset => 48;
    public static uint TextureIndexOffset => 64;


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
