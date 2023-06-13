#nullable enable
using System;

namespace Elffy;

internal sealed class EngineCoreException : Exception
{
    public EngineCoreException(string? message) : base(message)
    {
    }
}
