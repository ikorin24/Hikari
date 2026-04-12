#nullable enable
using Microsoft.CodeAnalysis;

namespace Hikari.Generator;

#pragma warning disable RS2008
internal static class DiagnosticDescriptors
{
    private const string Category = "Hikari.Generator";

    public static readonly DiagnosticDescriptor NotPartialStruct = new(
        id: "HKRG001",
        title: "Not partial struct",
        messageFormat: "The struct must be partial",
        category: Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotUnmanagedType = new(
        id: "HKRG002",
        title: "Not unmanaged type",
        messageFormat: "The struct must be unmanaged type",
        category: Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicatedStructLayout = new(
        id: "HKRG003",
        title: "Duplicated StructLayout",
        messageFormat: "The struct already has StructLayout attribute",
        category: Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotSupportedFieldType = new(
        id: "HKRG004",
        title: "Not supported field type",
        messageFormat: "The type '{0}' is not supported for field type",
        category: Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MultipleEntryPoints = new(
        id: "HKRG005",
        title: "Multiple entry points",
        messageFormat: "Multiple methods are marked with [EntryPoint]. Only one method can be the entry point, but '{0}' is also marked.",
        category: Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MultipleProjectFiles = new(
        id: "HKRG006",
        title: "Multiple project files",
        messageFormat: "Multiple project file (.hkrproj) exist",
        category: Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
#pragma warning restore RS2008
