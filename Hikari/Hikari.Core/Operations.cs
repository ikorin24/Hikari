#nullable enable
using Hikari.NativeBind;
using Hikari.UI;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hikari;

public sealed class Operations
{
    private readonly record struct OrderedOperation(Operation Operation, int SortOrder);

    private readonly Screen _screen;
    private readonly List<OrderedOperation> _list;
    private readonly List<OrderedOperation> _addedList;
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
        foreach(var (op, _) in _list.AsSpan()) {
            op.Terminate();
        }
        ApplyRemove();
    }

    internal void FrameInit()
    {
        Debug.Assert(_threadId.IsCurrentThread);
        foreach(var (op, _) in _list.AsSpan()) {
            op.InvokeFrameInit();
        }
    }

    internal void EarlyUpdate()
    {
        Debug.Assert(_threadId.IsCurrentThread);
        foreach(var (op, _) in _list.AsSpan()) {
            op.InvokeEarlyUpdate();
        }
    }

    internal void Update()
    {
        Debug.Assert(_threadId.IsCurrentThread);
        foreach(var (op, _) in _list.AsSpan()) {
            op.InvokeUpdate();
        }
    }

    internal void LateUpdate()
    {
        Debug.Assert(_threadId.IsCurrentThread);
        foreach(var (op, _) in _list.AsSpan()) {
            op.InvokeLateUpdate();
        }
    }

    internal void FrameEnd()
    {
        Debug.Assert(_threadId.IsCurrentThread);
        foreach(var (op, _) in _list.AsSpan()) {
            op.InvokeFrameEnd();
        }
    }

    internal void Execute(Rust.Ref<Wgpu.TextureView> surfaceView)
    {
        Debug.Assert(_threadId.IsCurrentThread);
        var screen = _screen;
        var lights = screen.Lights;
        {
            var context = new RenderShadowMapContext(lights);
            foreach(var (op, _) in _list.AsSpan()) {
                op.InvokeRenderShadowMap(in context);
            }
        }

        {
            var context = new OperationContext(screen, surfaceView);
            foreach(var (op, _) in _list.AsSpan()) {
                op.InvokeExecute(in context);
            }
        }
    }

    public PbrLayer AddPbrLayer(int sortOrder)
    {
        var op = new PbrLayer(_screen);
        Add(op, sortOrder);
        return op;
    }

    public DeferredProcess AddDeferredProcess(int sortOrder, IGBufferProvider gBufferProvider)
    {
        var op = new DeferredProcess(_screen, gBufferProvider);
        Add(op, sortOrder);
        return op;
    }

    public UITree AddUI(int sortOrder)
    {
        var op = new UILayer(_screen);
        Add(op, sortOrder);
        return new UITree(op);
    }

    private void Add(Operation operation, int sortOrder)
    {
        Debug.Assert(operation.LifeState == LifeState.New);
        lock(_sync) {
            _addedList.Add(new(operation, sortOrder));
        }
    }

    internal void ApplyAdd()
    {
        Debug.Assert(_threadId.IsCurrentThread);

        // To avoid deadlock, copy the added list to a local variable and add it to the '_list'.
        // This is because the user can call the 'Add' method during the Alive event of the added object.
        OrderedOperation[] addedList;
        lock(_sync) {
            if(_addedList.Count == 0) { return; }
            addedList = _addedList.AsReadOnlySpan().ToArray();
            _addedList.Clear();
        }
        _list.AddRange(addedList);
        _list.Sort((x, y) => x.SortOrder - y.SortOrder);
        foreach(var (added, _) in addedList) {
            added.SetLifeStateAlive();
            added.InvokeAlive();
        }

        foreach(var (op, _) in _list.AsSpan()) {
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

        foreach(var (op, _) in _list.AsSpan()) {
            if(op is ILazyApplyList laop) {
                laop.ApplyRemove();
            }
        }

        // To avoid deadlock, copy the removed list to a local variable and remove it from the '_list'.
        // This is because the user can call the 'Remove' method during the Dead event of the removed object.
        Operation[] removedList;
        lock(_sync) {
            if(_removedList.Count == 0) { return; }
            removedList = _removedList.AsReadOnlySpan().ToArray();
            _removedList.Clear();
        }
        foreach(var removed in removedList) {
            var index = -1;
            {
                var list = _list.AsSpan();
                for(var i = 0; i < list.Length; ++i) {
                    if(list[i].Operation == removed) {
                        index = i;
                        break;
                    }
                }
            }

            if(index >= 0) {
                _list.RemoveAt(index);
                removed.SetLifeStateDead();
                removed.InvokeRelease();
            }
        }
    }
}
