#nullable enable
using Elffy.Effective;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Elffy;

public sealed class RenderOperations
{
    // [NOTE]
    // The order of the elements in the list is not guaranteed.

    private readonly Screen _screen;
    private readonly List<RenderOperation> _list;
    private readonly List<RenderOperation> _addedList;
    private readonly List<RenderOperation> _removedList;
    private EventSource<RenderOperations> _added;
    private EventSource<RenderOperations> _removed;
    private readonly object _sync = new object();

    public Screen Screen => _screen;

    public Event<RenderOperations> Added => _added.Event;
    public Event<RenderOperations> Removed => _removed.Event;

    internal RenderOperations(Screen screen)
    {
        _screen = screen;
        _list = new();
        _addedList = new();
        _removedList = new();
    }

    internal void DisposeInternal()
    {
        // TODO:

    }

    internal void Render(in CommandEncoder encoder)
    {
        foreach(var op in _list.AsSpan()) {
            using var pass = op.GetRenderPass(encoder);
            op.InvokeRender(pass.AsValue());
        }
    }

    internal void Add(RenderOperation operation)
    {
        lock(_sync) {
            _addedList.Add(operation);
        }
    }

    internal void ApplyAdd()
    {
        var list = _list;
        var addedList = _addedList;
        var isAdded = false;
        lock(_sync) {
            if(addedList.Count > 0) {
                list.AddRange(addedList);
                list.Sort((x, y) => x.SortOrder - y.SortOrder);
                foreach(var addedItem in addedList.AsSpan()) {
                    addedItem.SetLifeStateAlive();
                }
                addedList.Clear();
                isAdded = true;
            }
        }

        foreach(var operation in list.AsSpan()) {
            operation.InvokeFrameInit();
        }

        if(isAdded) {
            _added.Invoke(this);
        }
    }

    internal void Remove(RenderOperation operation)
    {
        lock(_sync) {
            Debug.Assert(operation.LifeState == LifeState.Terminating);
            _removedList.Add(operation);
        }
    }

    internal void ApplyRemove()
    {
        var list = _list;
        var removedList = _removedList;
        var isRemoved = false;
        lock(_sync) {
            if(removedList.Count > 0) {
                foreach(var removedItem in removedList.AsSpan()) {
                    if(list.RemoveFastUnordered(removedItem)) {
                        removedItem.SetLifeStateDead();
                        removedItem.Release();
                    }
                }
                list.Sort((x, y) => x.SortOrder - y.SortOrder);
                removedList.Clear();
                isRemoved = true;
            }
        }

        foreach(var operation in list.AsSpan()) {
            operation.InvokeFrameEnd();
        }

        if(isRemoved) {
            _removed.Invoke(this);
        }
    }
}
