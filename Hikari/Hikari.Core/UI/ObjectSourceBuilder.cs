#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy.UI;

[InterpolatedStringHandler]
public ref struct ObjectSourceBuilder
{
    private DefaultInterpolatedStringHandler _handler;
    private List<Delegate>? _delegates;
    private List<Type>? _types;

    public ObjectSourceBuilder(int literalLength, int formattedCount)
    {
        _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
        _delegates = null;
        _types = null;
    }

    public void AppendLiteral(string s)
    {
        _handler.AppendLiteral(s);
    }

    public void AppendFormatted(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        _types ??= new List<Type>();
        var index = _types.Count;
        _types.Add(type);
        _handler.AppendLiteral("\"");
        _handler.AppendFormatted(index);
        _handler.AppendLiteral("@types\"");
    }

    public void AppendFormatted(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _delegates ??= new();
        var index = _delegates.Count;
        _delegates.Add(action);
        _handler.AppendLiteral("\"");
        _handler.AppendFormatted(index);
        _handler.AppendLiteral("@delegates\"");
    }

    public void AppendFormatted<T>(Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _delegates ??= new();
        var index = _delegates.Count;
        _delegates.Add(action);
        _handler.AppendLiteral("\"");
        _handler.AppendFormatted(index);
        _handler.AppendLiteral("@delegates\"");
    }

    public void AppendFormatted(sbyte value)
    {
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(byte value)
    {
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(short value)
    {
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(ushort value)
    {
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(int value)
    {
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(uint value)
    {
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(long value)
    {
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(ulong value)
    {
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(float value)
    {
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(double value)
    {
        _handler.AppendFormatted(value);
    }
    public void AppendFormatted(decimal value)
    {
        _handler.AppendFormatted(value);
    }

    public void AppendFormatted(string? value)
    {
        _handler.AppendLiteral("\"");
        _handler.AppendFormatted(value);
        _handler.AppendLiteral("\"");
    }

    public void AppendFormatted(scoped ReadOnlySpan<char> value)
    {
        _handler.AppendLiteral("\"");
        _handler.AppendFormatted(value);
        _handler.AppendLiteral("\"");
    }

    public void AppendFormatted<T>(T value, JsonMarker _ = default) where T : IToJson
    {
        Serializer.SerializeUtf16(value, ref this, (ReadOnlySpan<char> chars, ref ObjectSourceBuilder self) =>
        {
            self._handler.AppendFormatted(chars);
        });
    }

    public void AppendFormatted<T>(T value, EnumMarker _ = default) where T : struct, Enum
    {
        _handler.AppendFormatted(value.ToString());
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public record struct EnumMarker;
    [EditorBrowsable(EditorBrowsableState.Never)]
    public record struct JsonMarker;

    public ObjectSource ToSourceClear()
    {
        return new ObjectSource(_handler.ToStringAndClear(), _delegates, _types);
    }

    public static implicit operator ObjectSourceBuilder([StringSyntax(StringSyntaxAttribute.Json)] string s)
    {
        var h = new ObjectSourceBuilder(s?.Length ?? 0, 0);
        h.AppendLiteral(s ?? "");
        return h;
    }
}
