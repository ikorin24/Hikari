#nullable enable
using Elffy.Effective;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Elffy;

public abstract class RenderOperation<TSelf, TShader, TMaterial>
    : Operation
    where TSelf : RenderOperation<TSelf, TShader, TMaterial>
    where TShader : Shader<TShader, TMaterial, TSelf>
    where TMaterial : Material<TMaterial, TShader, TSelf>
{
    private readonly Own<PipelineLayout> _pipelineLayout;

    public PipelineLayout PipelineLayout => _pipelineLayout.AsValue();

    protected RenderOperation(Screen screen, Own<PipelineLayout> pipelineLayout, int sortOrder)
        : base(screen, sortOrder)
    {
        pipelineLayout.ThrowArgumentExceptionIfNone();
        _pipelineLayout = pipelineLayout;
    }

    protected sealed override void Execute(in OperationContext context)
    {
        using var pass = CreateRenderPass(in context);
        Render(pass.AsValue());
    }

    protected abstract OwnRenderPass CreateRenderPass(in OperationContext context);

    protected abstract void Render(in RenderPass pass);
}

public abstract class ObjectLayer<TSelf, TVertex, TShader, TMaterial, TObject>
    : RenderOperation<TSelf, TShader, TMaterial>
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

    protected ObjectLayer(Screen screen, Own<PipelineLayout> pipelineLayout, int sortOrder)
        : base(screen, pipelineLayout, sortOrder)
    {
        _list = new();
        _addedList = new();
        _removedList = new();
    }

    protected sealed override void RenderShadowMap(in RenderShadowMapContext context)
    {
        RenderShadowMap(context, _list.AsSpan());
    }

    protected abstract void RenderShadowMap(in RenderShadowMapContext context, ReadOnlySpan<TObject> objects);

    internal void Add(TObject frameObject)
    {
        lock(_sync) {
            _addedList.Add(frameObject);
        }
    }

    protected sealed override void FrameInit()
    {
        ApplyAdd();
    }

    private void ApplyAdd()
    {
        var addedList = _addedList;
        lock(_sync) {
            if(addedList.Count == 0) {
                return;
            }
            _list.AddRange(addedList);
            foreach(var addedObject in addedList.AsSpan()) {
                addedObject.SetLifeStateAlive();
                addedObject.OnAlive();
            }
            addedList.Clear();
        }
    }

    internal void Remove(TObject frameObject)
    {
        lock(_sync) {
            Debug.Assert(frameObject.LifeState == LifeState.Terminating);
            _removedList.Add(frameObject);
        }
    }

    protected sealed override void FrameEnd()
    {
        ApplyRemove();
    }

    private void ApplyRemove()
    {
        var list = _list;
        var removedList = _removedList;
        lock(_sync) {
            if(removedList.Count == 0) {
                return;
            }
            foreach(var removedItem in removedList.AsSpan()) {
                if(list.RemoveFastUnordered(removedItem)) {
                    removedItem.SetLifeStateDead();
                    removedItem.OnDead();
                }
            }
            removedList.Clear();
        }
    }

    protected sealed override void Render(in RenderPass pass)
    {
        ReadOnlySpan<TObject> objects = _list.AsSpan();
        Render(
            pass,
            objects,
            (in RenderPass pass, TObject obj) => obj.InvokeRender(in pass));
    }

    public delegate void RenderObjectAction(in RenderPass pass, TObject obj);

    protected virtual void Render(in RenderPass pass, ReadOnlySpan<TObject> objects, RenderObjectAction render)
    {
        foreach(var obj in objects) {
            render(in pass, obj);
        }
    }

    protected sealed override void EarlyUpdate()
    {
        foreach(var obj in _list.AsSpan()) {
            if(obj.IsFrozen) { continue; }
            obj.InvokeEarlyUpdate();
        }
    }

    protected sealed override void LateUpdate()
    {
        foreach(var obj in _list.AsSpan()) {
            if(obj.IsFrozen) { continue; }
            obj.InvokeLateUpdate();
        }
    }

    protected sealed override void Update()
    {
        foreach(var obj in _list.AsSpan()) {
            if(obj.IsFrozen) { continue; }
            obj.InvokeUpdate();
        }
    }
}
