#nullable enable
using System.Diagnostics;

namespace Hikari;

[DebuggerDisplay("{DebugView,nq}")]
public readonly record struct PassKind
{
    private readonly KindInternal _kind;
    private readonly string? _customName;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebugView => _kind switch
    {
        KindInternal.Surface => nameof(Surface),
        KindInternal.ShadowMap => nameof(ShadowMap),
        KindInternal.GBuffer => nameof(GBuffer),
        KindInternal.Custom => $"{nameof(Custom)}(\"{_customName}\")",
        _ => "",
    };

    public static PassKind Surface => new(KindInternal.Surface);

    public static PassKind GBuffer => new(KindInternal.GBuffer);

    public static PassKind ShadowMap => new(KindInternal.ShadowMap);

    public static PassKind Custom(string name) => new(name);

    internal KindInternal Kind => _kind;
    internal string? CustomName => _customName;

    private PassKind(KindInternal kind)
    {
        _kind = kind;
        _customName = null;
    }

    public PassKind(string customName)
    {
        _kind = KindInternal.Custom;
        _customName = customName;
    }

    public override string ToString() => DebugView;

    internal enum KindInternal
    {
        Surface = 0,
        ShadowMap,
        GBuffer,
        Custom,
    }
}
