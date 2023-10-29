#nullable enable
using Hikari.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hikari;

internal sealed class ObjectStore
{
    private readonly List<FrameObject> _list;
    private readonly List<FrameObject> _addedList;
    private readonly List<FrameObject> _removedList;
    private readonly object _sync = new object();

    private readonly Screen _screen;

    public Screen Screen => _screen;

    internal ObjectStore(Screen screen)
    {
        _screen = screen;
        _list = new();
        _addedList = new();
        _removedList = new();
    }

    internal void OnClosed()
    {
        Debug.Assert(_screen.MainThread.IsCurrentThread);
        foreach(var obj in _list.AsSpan()) {
            obj.Terminate();
        }
        ApplyRemove();
    }

    internal void Add(FrameObject frameObject)
    {
        Debug.Assert(frameObject.LifeState == LifeState.New);
        lock(_sync) {
            _addedList.Add(frameObject);
        }
    }

    internal void ApplyAdd()
    {
        Debug.Assert(Screen.MainThread.IsCurrentThread);

        // To avoid deadlock, copy the added list to a local variable and add it to the '_list'.
        // This is because the user can call the 'Add' method during the Alive event of the added object.
        RefTypeRentMemory<FrameObject> tmp;
        lock(_sync) {
            if(_addedList.Count == 0) { return; }
            tmp = new RefTypeRentMemory<FrameObject>(_addedList.AsReadOnlySpan());
            _addedList.Clear();
        }
        try {
            var addedList = tmp.AsReadOnlySpan();
            foreach(var addedObject in addedList) {
                if(addedObject.LifeState != LifeState.New) { continue; }
                _list.Add(addedObject);
            }
            foreach(var addedObject in addedList) {
                if(addedObject.LifeState != LifeState.New) { continue; }
                addedObject.SetLifeStateAlive();
                addedObject.OnAlive();
            }
        }
        finally {
            tmp.Dispose();
        }
    }

    internal void Remove(FrameObject frameObject)
    {
        Debug.Assert(frameObject.LifeState == LifeState.Terminating);
        lock(_sync) {
            _removedList.Add(frameObject);
        }
    }

    internal void ApplyRemove()
    {
        Debug.Assert(Screen.MainThread.IsCurrentThread);

        // To avoid deadlock, copy the removed list to a local variable and remove it from the '_list'.
        // This is because the user can call the 'Remove' method during the Dead event of the removed object.
        RefTypeRentMemory<FrameObject> tmp;
        lock(_sync) {
            if(_removedList.Count == 0) { return; }
            tmp = new RefTypeRentMemory<FrameObject>(_removedList.AsReadOnlySpan());
            _removedList.Clear();
        }
        try {
            var removedList = tmp.AsReadOnlySpan();
            foreach(var removedItem in removedList) {
                if(_list.SwapRemove(removedItem)) {
                    removedItem.SetLifeStateDead();
                    removedItem.OnDead();
                }
            }
        }
        finally {
            tmp.Dispose();
        }
    }

    internal void UseObjects<T>(T arg, ReadOnlySpanAction<FrameObject, T> action)
    {
        Debug.Assert(Screen.MainThread.IsCurrentThread);
        var objects = _list.AsSpan();
        action.Invoke(objects, arg);
    }

    internal void UseObjects(ReadOnlySpanAction<FrameObject> action)
    {
        Debug.Assert(Screen.MainThread.IsCurrentThread);
        var objects = _list.AsSpan();
        action.Invoke(objects);
    }
}
