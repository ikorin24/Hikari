#nullable enable
using Elffy.Effective;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Elffy;

public abstract class RenderOperation : Operation
{
    private readonly Own<RenderPipeline> _pipeline;

    protected RenderOperation(Screen screen, Own<RenderPipeline> pipeline, int sortOrder) : base(screen, sortOrder)
    {
        _pipeline = pipeline;
        screen.Operations.Add(this);
    }

    protected sealed override void Execute(in CommandEncoder encoder)
    {
        using var pass = CreateRenderPass(in encoder);
        Render(pass.AsValue(), _pipeline.AsValue());
    }

    protected virtual Own<RenderPass> CreateRenderPass(in CommandEncoder encoder)
    {
        return RenderPass.SurfaceRenderPass(in encoder);
    }

    protected abstract void Render(in RenderPass pass, RenderPipeline pipeline);
}

public abstract class RenderOperation<TShader, TMaterial>
    : RenderOperation
    where TShader : Shader<TShader, TMaterial>
    where TMaterial : Material<TMaterial, TShader>
{
    private readonly Own<TShader> _shader;

    public TShader Shader => _shader.AsValue();

    protected RenderOperation(Own<TShader> shader, Own<RenderPipeline> pipeline, int sortOrder)
        : base(shader.AsValue().Screen, pipeline, sortOrder)
    {
        shader.ThrowArgumentExceptionIfNone();
        _shader = shader;
    }
}

public abstract class ObjectLayer<TSelf, TVertex, TShader, TMaterial>
    : RenderOperation<TShader, TMaterial>
    where TSelf : ObjectLayer<TSelf, TVertex, TShader, TMaterial>
    where TVertex : unmanaged, IVertex
    where TShader : Shader<TShader, TMaterial>
    where TMaterial : Material<TMaterial, TShader>
{
    private readonly List<FrameObject<TSelf, TVertex, TShader, TMaterial>> _list;
    private readonly List<FrameObject<TSelf, TVertex, TShader, TMaterial>> _addedList;
    private readonly List<FrameObject<TSelf, TVertex, TShader, TMaterial>> _removedList;
    private readonly object _sync = new object();

    protected ObjectLayer(Own<TShader> shader, Func<TShader, RenderPipelineDescriptor> pipelineDescGen, int sortOrder)
        : this(shader, pipelineDescGen(shader.AsValue()), sortOrder) { }

    protected ObjectLayer(Own<TShader> shader, Func<TShader, Own<RenderPipeline>> pipelineGen, int sortOrder)
        : this(shader, pipelineGen(shader.AsValue()), sortOrder) { }

    protected ObjectLayer(Own<TShader> shader, in RenderPipelineDescriptor pipelineDesc, int sortOrder)
        : this(shader, RenderPipeline.Create(shader.AsValue().Screen, pipelineDesc), sortOrder) { }

    protected ObjectLayer(Own<TShader> shader, Own<RenderPipeline> pipeline, int sortOrder)
        : base(shader, pipeline, sortOrder)
    {
        _list = new();
        _addedList = new();
        _removedList = new();
    }

    internal void Add(FrameObject<TSelf, TVertex, TShader, TMaterial> frameObject)
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
            }
            addedList.Clear();
        }
    }

    internal void Remove(FrameObject<TSelf, TVertex, TShader, TMaterial> frameObject)
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

    protected sealed override void Render(in RenderPass pass, RenderPipeline pipeline)
    {
        pass.SetPipeline(pipeline);
        foreach(var obj in _list.AsSpan()) {
            if(obj is Renderable<TSelf, TVertex, TShader, TMaterial> renderable) {
                renderable.InvokeRender(pass);
            }
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
