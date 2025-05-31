#nullable enable
using System;
using System.Collections.Generic;

namespace Hikari.Internal;

internal sealed class DisposableBag : IDisposable
{
    private readonly List<IDisposable> _list;

    public DisposableBag()
    {
        _list = new List<IDisposable>();
    }

    public DisposableBag(int capacity)
    {
        _list = new List<IDisposable>(capacity);
    }

    public void Add(IDisposable item)
    {
        _list.Add(item);
    }

    public void Dispose()
    {
        foreach(var item in _list.AsSpan()) {
            item.Dispose();
        }
        _list.Clear();
    }
}

internal static class DisposableBagExtensions
{
    public static void AddTo(this IDisposable item, DisposableBag bag) => bag.Add(item);
    public static T AddTo<T>(this Own<T> item, DisposableBag bag) where T : notnull
    {
        bag.Add(item);
        return item.AsValue();
    }
    public static T AddTo<T>(this MaybeOwn<T> item, DisposableBag bag) where T : notnull
    {
        bag.Add(item);
        return item.AsValue();
    }
}

internal static class DisposableExtensions
{
    public static void DisposeOn<TDisposable, _>(this TDisposable self, Event<_> lifetimeLimit) where TDisposable : IDisposable
    {
        lifetimeLimit.Subscribe(_ => self.Dispose());
    }

    public static void DisposeOn<_>(this IDisposable self, Event<_> lifetimeLimit)
    {
        lifetimeLimit.Subscribe(_ => self.Dispose());
    }
}
