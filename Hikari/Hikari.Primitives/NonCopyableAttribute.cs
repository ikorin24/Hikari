#nullable enable
using System;

namespace Hikari
{
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class NonCopyableAttribute : Attribute { }
}
