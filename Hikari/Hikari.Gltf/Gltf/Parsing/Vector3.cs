﻿#nullable enable
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hikari.Gltf.Parsing;

[JsonConverter(typeof(Vector3Converter))]
[DebuggerDisplay("({X}, {Y}, {Z})")]
internal struct Vector3
{
    public float X;
    public float Y;
    public float Z;

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static implicit operator Hikari.Vector3(Vector3 v) => new Hikari.Vector3(v.X, v.Y, v.Z);
}

internal sealed class Vector3Converter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // expected [number, number, number]

        if(reader.TokenType != JsonTokenType.StartArray) { throw new JsonException(); }
        reader.Read();
        if(reader.TokenType != JsonTokenType.Number) { throw new JsonException(); }
        Vector3 value;
        value.X = reader.GetSingle();
        reader.Read();
        value.Y = reader.GetSingle();
        reader.Read();
        value.Z = reader.GetSingle();
        reader.Read();

        if(reader.TokenType != JsonTokenType.EndArray) { throw new JsonException(); }
        return value;
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options) => throw new NotSupportedException();
}
