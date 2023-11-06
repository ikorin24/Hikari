#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hikari;

internal sealed class ShaderPassList
{
    private readonly Screen _screen;
    private readonly List<ShaderPass> _list;
    private readonly List<ShaderPass> _added;
    private readonly List<ShaderPass> _removed;
    private readonly object _sync = new();

    public Screen Screen => _screen;

    internal ShaderPassList(Screen screen)
    {
        _screen = screen;
        _list = new List<ShaderPass>();
        _added = new List<ShaderPass>();
        _removed = new List<ShaderPass>();
    }

    internal void OnClosed()
    {
        Debug.Assert(Screen.MainThread.IsCurrentThread);
        var all = _list.AsSpan();
        Remove(all);
        ApplyRemove();
    }

    public void Add(ReadOnlySpan<ShaderPass> passes)
    {
        lock(_sync) {
            _added.AddRange(passes);
        }
    }

    public void Remove(ReadOnlySpan<ShaderPass> passes)
    {
        lock(_sync) {
            _removed.AddRange(passes);
        }
    }

    public void ApplyAdd()
    {
        var needToSort = false;
        lock(_sync) {
            if(_added.Count > 0) {
                _list.AddRange(_added);
                _added.Clear();
                needToSort = true;
            }
        }
        if(needToSort) {
            _list.Sort((a, b) => a.SortOrder - b.SortOrder);
        }
    }

    public void ApplyRemove()
    {
        lock(_sync) {
            if(_removed.Count > 0) {
                foreach(var pass in _removed.AsSpan()) {
                    _list.Remove(pass);
                }
                _removed.Clear();
            }
        }
    }

    public void Execute()
    {
        foreach(var pass in _list.AsSpan()) {
            pass.Execute();
        }
    }
}
