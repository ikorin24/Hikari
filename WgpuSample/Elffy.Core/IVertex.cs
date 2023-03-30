#nullable enable
using System;

namespace Elffy;

public interface IVertex<TSelf>
    where TSelf : unmanaged, IVertex<TSelf>
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
