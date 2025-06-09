#nullable enable

namespace Hikari;

internal static class EngineConsts
{
    /// <summary>
    /// Buffer-Texture copies must have `bytes_per_row` aligned to this number.
    /// </summary>
    public const u32 COPY_BYTES_PER_ROW_ALIGNMENT = 256;
    /// <summary>
    /// An offset into the query resolve buffer has to be aligned to this.
    /// </summary>
    public const u64 QUERY_RESOLVE_BUFFER_ALIGNMENT = 256;
    /// <summary>
    /// Buffer to buffer copy as well as buffer clear offsets and sizes must be aligned to this number.
    /// </summary>
    public const u64 COPY_BUFFER_ALIGNMENT = 4;
    /// <summary>
    /// Size to align mappings.
    /// </summary>
    public const u64 MAP_ALIGNMENT = 8;
    /// <summary>
    /// Vertex buffer strides have to be aligned to this number.
    /// </summary>
    public const u64 VERTEX_STRIDE_ALIGNMENT = 4;
    /// <summary>
    /// Alignment all push constants need
    /// </summary>
    public const u32 PUSH_CONSTANT_ALIGNMENT = 4;
    /// <summary>
    /// Maximum queries in a query set
    /// </summary>
    public const u32 QUERY_SET_MAX_QUERIES = 4096;
    /// <summary>
    /// Size of a single piece of query data.
    /// </summary>
    public const u32 QUERY_SIZE = 8;
}
