#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hikari;

internal sealed class ObjectStore
{
    private readonly List<FrameObject> _list;
    private readonly Dictionary<FrameObject, int> _indexDic;
    private readonly List<FrameObject> _addedTmp;
    private readonly List<FrameObject> _removedTmp;
    private bool _iterating = false;

    private readonly Screen _screen;

    public Screen Screen => _screen;

    internal ObjectStore(Screen screen)
    {
        _screen = screen;
        _list = [];
        _indexDic = [];
        _addedTmp = [];
        _removedTmp = [];
    }

    internal void TerminateAll()
    {
        Debug.Assert(_screen.MainThread.IsCurrentThread);
        UseObjects(static objects =>
        {
            foreach(var obj in objects) {
                obj.Terminate();
            }
        });
    }

    internal void Add(FrameObject frameObject)
    {
        Debug.Assert(_screen.MainThread.IsCurrentThread);
        Debug.Assert(frameObject.LifeState == LifeState.New);
        if(_iterating) {
            _addedTmp.Add(frameObject);
        }
        else {
            _list.Add(frameObject);
            _indexDic.Add(frameObject, _list.Count - 1);

            frameObject.SetLifeStateAlive();
            frameObject.OnAlive();
        }
    }

    internal void Remove(FrameObject frameObject)
    {
        Debug.Assert(Screen.MainThread.IsCurrentThread);
        Debug.Assert(frameObject.LifeState == LifeState.Terminating);
        if(_iterating) {
            _removedTmp.Add(frameObject);
        }
        else {
            if(_indexDic.TryGetValue(frameObject, out var index)) {
                var lastItem = _list[^1];
                if(_list.SwapRemoveAt(index)) {
                    _indexDic[lastItem] = index;
                    _indexDic.Remove(frameObject);
                }
                frameObject.SetLifeStateDead();
                frameObject.OnDead();
            }
            else {
                Debug.Fail(null);
            }
        }
    }

    private void ProcessPendingObjects()
    {
        var removedTmp = _removedTmp;
        while(removedTmp.TryPop(out var obj)) {
            Remove(obj);
        }
        var addedTmp = _addedTmp;
        while(addedTmp.TryPop(out var obj)) {
            Add(obj);
        }
    }

    internal void UseObjects<T>(T arg, ReadOnlySpanAction<FrameObject, T> action)
        where T : allows ref struct
    {
        Debug.Assert(Screen.MainThread.IsCurrentThread);
        var objects = _list.AsSpan();
        if(_iterating) {
            throw new InvalidOperationException("invalid reentrant");
        }
        _iterating = true;
        try {
            action.Invoke(objects, arg);
        }
        finally {
            _iterating = false;
            ProcessPendingObjects();
        }
    }

    internal void UseObjects(ReadOnlySpanAction<FrameObject> action)
    {
        Debug.Assert(Screen.MainThread.IsCurrentThread);
        var objects = _list.AsSpan();
        if(_iterating) {
            throw new InvalidOperationException("invalid reentrant");
        }
        _iterating = true;
        try {
            action.Invoke(objects);
        }
        finally {
            _iterating = false;
            ProcessPendingObjects();
        }
    }
}
