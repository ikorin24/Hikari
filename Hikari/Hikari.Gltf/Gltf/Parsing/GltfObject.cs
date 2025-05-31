#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hikari.Gltf.Parsing;

// https://github.com/KhronosGroup/glTF-Tutorials
// https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html

internal sealed class GltfObject
{
#pragma warning disable IDE1006 // naming rule

    public U8String[]? extensionsUsed { get; set; }
    public U8String[]? extensionsRequired { get; set; }
    public Accessor[]? accessors { get; set; }
    public Animation[]? animations { get; set; }
    public Asset asset { get; set; }   // must
    public Buffer[]? buffers { get; set; }
    public BufferView[]? bufferViews { get; set; }
    public Camera[]? cameras { get; set; }
    public Image[]? images { get; set; }
    public Material[]? materials { get; set; }
    public Mesh[]? meshes { get; set; }
    public Node[]? nodes { get; set; }
    public Sampler[]? samplers { get; set; }
    public uint? scene { get; set; }
    public Scene[]? scenes { get; set; }
    public Skin[]? skins { get; set; }
    public Texture[]? textures { get; set; }

#pragma warning restore IDE1006 // naming rule
}

[JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true, Converters = [typeof(NUIntConverter)])]
[JsonSerializable(typeof(GltfObject))]
internal partial class GltfObjectContext : JsonSerializerContext
{
}

internal sealed class NUIntConverter : JsonConverter<nuint>
{
    public override nuint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return checked((nuint)reader.GetUInt64());
    }

    public override void Write(Utf8JsonWriter writer, nuint value, JsonSerializerOptions options) => throw new NotSupportedException();
}
