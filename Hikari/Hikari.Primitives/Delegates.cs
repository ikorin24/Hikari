#nullable enable
using System;
using Cysharp.Threading.Tasks;

namespace Hikari
{
    public delegate void SpanAction<T>(Span<T> span);
    public delegate void SpanAction<T, in TArg>(Span<T> span, TArg arg) where TArg : allows ref struct;
    public delegate void ReadOnlySpanAction<T>(ReadOnlySpan<T> span);
    public delegate void ReadOnlySpanAction<T, in TArg>(ReadOnlySpan<T> span, TArg arg) where TArg : allows ref struct;

    public delegate TResult SpanFunc<T, out TResult>(Span<T> span);
    public delegate TResult SpanFunc<T, in TArg, out TResult>(Span<T> span, TArg arg) where TArg : allows ref struct;
    public delegate TResult ReadOnlySpanFunc<T, out TResult>(ReadOnlySpan<T> span);
    public delegate TResult ReadOnlySpanFunc<T, in TArg, out TResult>(ReadOnlySpan<T> span, TArg arg) where TArg : allows ref struct;

    public delegate UniTask AsyncSpanAction<T>(Span<T> span);
    public delegate UniTask AsyncSpanAction<T, in TArg>(Span<T> span, TArg arg) where TArg : allows ref struct;
    public delegate UniTask AsyncReadOnlySpanAction<T>(ReadOnlySpan<T> span);
    public delegate UniTask AsyncReadOnlySpanAction<T, in TArg>(ReadOnlySpan<T> span, TArg arg) where TArg : allows ref struct;

    public delegate UniTask<TResult> AsyncSpanFunc<T, TResult>(Span<T> span);
    public delegate UniTask<TResult> AsyncSpanFunc<T, in TArg, TResult>(Span<T> span, TArg arg) where TArg : allows ref struct;
    public delegate UniTask<TResult> AsyncReadOnlySpanFunc<T, TResult>(ReadOnlySpan<T> span);
    public delegate UniTask<TResult> AsyncReadOnlySpanFunc<T, in TArg, TResult>(ReadOnlySpan<T> span, TArg arg) where TArg : allows ref struct;
}
