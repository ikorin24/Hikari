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
