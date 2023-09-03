#nullable enable
using Hikari.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hikari;

public abstract class ObjectLayer<TSelf, TVertex, TShader, TMaterial, TObject>
    : RenderOperation<TSelf, TShader, TMaterial>,
      ILazyApplyList
    where TSelf : ObjectLayer<TSelf, TVertex, TShader, TMaterial, TObject>
    where TVertex : unmanaged, IVertex
    where TShader : Shader<TShader, TMaterial, TSelf>
    where TMaterial : Material<TMaterial, TShader, TSelf>
    where TObject : FrameObject<TObject, TSelf, TVertex, TShader, TMaterial>
{
    private readonly List<TObject> _list;
    private readonly List<TObject> _addedList;
    private readonly List<TObject> _removedList;
    private readonly object _sync = new object();

    private protected ObjectLayer(Screen screen, Own<PipelineLayout> pipelineLayout)
        : base(screen, pipelineLayout)
    {
        _list = new();
        _addedList = new();
        _removedList = new();

        EarlyUpdate.Subscribe(static self => ((TSelf)self).OnEarlyUpdate());
        Update.Subscribe(static self => ((TSelf)self).OnUpdate());
        LateUpdate.Subscribe(static self => ((TSelf)self).OnLateUpdate());
        FrameInit.Subscribe(static self => ((TSelf)self).OnFrameInit());
        FrameEnd.Subscribe(static self => ((TSelf)self).OnFrameEnd());

        Terminated.Subscribe(static self =>
        {
            ((TSelf)self).OnTerminated();
        });
    }

    private void OnTerminated()
    {
        Debug.Assert(Screen.MainThread.IsCurrentThread);
        foreach(var obj in _list.AsSpan()) {
            obj.Terminate();
        }
    }

    protected sealed override void RenderShadowMap(in RenderShadowMapContext context)
    {
        Debug.Assert(Screen.MainThread.IsCurrentThread);
        RenderShadowMap(context, _list.AsSpan());
    }

    protected abstract void RenderShadowMap(in RenderShadowMapContext context, ReadOnlySpan<TObject> objects);

    internal void Add(TObject frameObject)
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
        RefTypeRentMemory<TObject> tmp;
        lock(_sync) {
            if(_addedList.Count == 0) { return; }
            tmp = new RefTypeRentMemory<TObject>(_addedList.AsReadOnlySpan());
            _addedList.Clear();
        }
        try {
            var addedList = tmp.AsReadOnlySpan();
            _list.AddRange(addedList);
            foreach(var addedObject in addedList) {
                addedObject.SetLifeStateAlive();
                addedObject.OnAlive();
            }
        }
        finally {
            tmp.Dispose();
        }
    }

    internal void Remove(TObject frameObject)
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
        RefTypeRentMemory<TObject> tmp;
        lock(_sync) {
            if(_removedList.Count == 0) { return; }
            tmp = new RefTypeRentMemory<TObject>(_removedList.AsReadOnlySpan());
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

    protected sealed override void Render(in RenderPass pass)
    {
        Debug.Assert(Screen.MainThread.IsCurrentThread);
        ReadOnlySpan<TObject> objects = _list.AsSpan();
        Render(
            pass,
            objects,
            (in RenderPass pass, TObject obj) => obj.OnRender(in pass));
    }

    public delegate void RenderObjectAction(in RenderPass pass, TObject obj);

    protected virtual void Render(in RenderPass pass, ReadOnlySpan<TObject> objects, RenderObjectAction render)
    {
        foreach(var obj in objects) {
            render(in pass, obj);
        }
    }

    private void OnFrameInit()
    {
        Debug.Assert(Screen.MainThread.IsCurrentThread);
        ApplyAdd();
    }

    private void OnEarlyUpdate()
    {
        Debug.Assert(Screen.MainThread.IsCurrentThread);
        foreach(var obj in _list.AsSpan()) {
            if(obj.IsFrozen) { continue; }
            obj.InvokeEarlyUpdate();
        }
    }

    private void OnLateUpdate()
    {
        Debug.Assert(Screen.MainThread.IsCurrentThread);
        foreach(var obj in _list.AsSpan()) {
            if(obj.IsFrozen) { continue; }
            obj.InvokeLateUpdate();
        }
    }

    private void OnUpdate()
    {
        Debug.Assert(Screen.MainThread.IsCurrentThread);
        foreach(var obj in _list.AsSpan()) {
            if(obj.IsFrozen) { continue; }
            obj.InvokeUpdate();
        }
    }

    private void OnFrameEnd()
    {
        Debug.Assert(Screen.MainThread.IsCurrentThread);
        ApplyRemove();
    }

    void ILazyApplyList.ApplyAdd() => ApplyAdd();

    void ILazyApplyList.ApplyRemove() => ApplyRemove();
}

internal interface ILazyApplyList
{
    void ApplyAdd();
    void ApplyRemove();
}
