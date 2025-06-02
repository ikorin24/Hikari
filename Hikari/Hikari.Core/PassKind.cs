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
        KindInternal.Forward => nameof(Forward),
        KindInternal.ShadowMap => nameof(ShadowMap),
        KindInternal.Deferred => nameof(Deferred),
        KindInternal.Custom => $"{nameof(Custom)}(\"{_customName}\")",
        _ => "",
    };

    public static PassKind Forward => new(KindInternal.Forward);

    public static PassKind Deferred => new(KindInternal.Deferred);

    public static PassKind ShadowMap => new(KindInternal.ShadowMap);

    public static PassKind Custom(string name) => new(name);

    internal KindInternal Kind => _kind;
    internal string? CustomName => _customName;

    private PassKind(KindInternal kind)
    {
        _kind = kind;
        _customName = null;
    }

    private PassKind(string customName)
    {
        _kind = KindInternal.Custom;
        _customName = customName;
    }

    public override string ToString() => DebugView;

    internal enum KindInternal
    {
        Forward = 0,
        ShadowMap,
        Deferred,
        Custom,
    }
}
