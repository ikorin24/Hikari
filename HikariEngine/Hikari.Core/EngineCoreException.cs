#nullable enable
using System;

namespace Hikari;

internal sealed class EngineCoreException : Exception
{
    public EngineCoreException(string? message) : base(message)
    {
    }
}
