#nullable enable
using Hikari;
using Hikari.Collections;
using Hikari.NativeBind;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hikari;

public sealed class Operations
{
    // [NOTE]
    // The order of the elements in the list is not guaranteed.

    private readonly Screen _screen;
    private readonly List<Operation> _list;
    private readonly List<Operation> _addedList;
    private readonly List<Operation> _removedList;
    private readonly ThreadId _threadId;
    private readonly object _sync = new object();

    public Screen Screen => _screen;

    internal Operations(Screen screen)
    {
        _screen = screen;
        _list = new();
        _addedList = new();
        _removedList = new();
        _threadId = ThreadId.CurrentThread();
    }

    internal void OnClosed()
    {
        Debug.Assert(_threadId.IsCurrentThread);
        foreach(var op in _list.AsSpan()) {
            op.Terminate();
        }
        ApplyRemove();
    }

    internal void FrameInit()
    {
        Debug.Assert(_threadId.IsCurrentThread);
        foreach(var operation in _list.AsSpan()) {
            operation.InvokeFrameInit();
        }
    }

    internal void EarlyUpdate()
    {
        Debug.Assert(_threadId.IsCurrentThread);
        foreach(var op in _list.AsSpan()) {
            op.InvokeEarlyUpdate();
        }
    }

    internal void Update()
    {
        Debug.Assert(_threadId.IsCurrentThread);
        foreach(var op in _list.AsSpan()) {
            op.InvokeUpdate();
        }
    }

    internal void LateUpdate()
    {
        Debug.Assert(_threadId.IsCurrentThread);
        foreach(var op in _list.AsSpan()) {
            op.InvokeLateUpdate();
        }
    }

    internal void FrameEnd()
    {
        Debug.Assert(_threadId.IsCurrentThread);
        foreach(var operation in _list.AsSpan()) {
            operation.InvokeFrameEnd();
        }
    }

    internal void Execute(Rust.Ref<Wgpu.TextureView> surfaceView)
    {
        Debug.Assert(_threadId.IsCurrentThread);
        var screen = _screen;
        var lights = screen.Lights;
        {
            var context = new RenderShadowMapContext(lights);
            foreach(var op in _list.AsSpan()) {
                op.InvokeRenderShadowMap(in context);
            }
        }

        {
            var context = new OperationContext(screen, surfaceView);
            foreach(var op in _list.AsSpan()) {
                op.InvokeExecute(in context);
            }
        }
    }

    internal void Add(Operation operation)
    {
        Debug.Assert(operation.LifeState == LifeState.New);
        lock(_sync) {
            _addedList.Add(operation);
        }
    }

    internal void ApplyAdd()
    {
        Debug.Assert(_threadId.IsCurrentThread);

        // To avoid deadlock, copy the added list to a local variable and add it to the '_list'.
        // This is because the user can call the 'Add' method during the Alive event of the added object.
        RefTypeRentMemory<Operation> tmp;
        lock(_sync) {
            if(_addedList.Count == 0) { return; }
            tmp = new RefTypeRentMemory<Operation>(_addedList.AsReadOnlySpan());
            _addedList.Clear();
        }
        try {
            var addedList = tmp.AsReadOnlySpan();
            _list.AddRange(addedList);
            _list.Sort((x, y) => x.SortOrder - y.SortOrder);
            foreach(var added in addedList) {
                added.SetLifeStateAlive();
                added.InvokeAlive();
            }
        }
        finally {
            tmp.Dispose();
        }

        foreach(var op in _list.AsSpan()) {
            if(op is ILazyApplyList laop) {
                laop.ApplyAdd();
            }
        }
    }

    internal void Remove(Operation operation)
    {
        Debug.Assert(operation.LifeState == LifeState.Terminating);
        lock(_sync) {
            _removedList.Add(operation);
        }
    }

    internal void ApplyRemove()
    {
        Debug.Assert(_threadId.IsCurrentThread);

        foreach(var op in _list.AsSpan()) {
            if(op is ILazyApplyList laop) {
                laop.ApplyRemove();
            }
        }

        // To avoid deadlock, copy the removed list to a local variable and remove it from the '_list'.
        // This is because the user can call the 'Remove' method during the Dead event of the removed object.
        RefTypeRentMemory<Operation> tmp;
        lock(_sync) {
            if(_removedList.Count == 0) { return; }
            tmp = new RefTypeRentMemory<Operation>(_removedList.AsReadOnlySpan());
            _removedList.Clear();
        }
        try {
            var removedList = tmp.AsReadOnlySpan();
            foreach(var removed in removedList) {
                if(_list.RemoveFastUnordered(removed)) {
                    removed.SetLifeStateDead();
                    removed.InvokeRelease();
                }
            }
        }
        finally {
            tmp.Dispose();
        }
    }
}
