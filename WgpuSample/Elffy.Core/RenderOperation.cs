#nullable enable
using Elffy.Effective;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy;

public abstract class RenderOperation
{
    private readonly Screen _screen;
    private readonly Own<RenderPipeline> _pipelineOwn;
    private LifeState _lifeState;
    private readonly SubscriptionBag _subscriptions = new();
    private EventSource<RenderOperation> _onDead = new();

    public Screen Screen => _screen;
    public RenderPipeline Pipeline => _pipelineOwn.AsValue();
    public LifeState LifeState => _lifeState;
    public SubscriptionRegister Subscriptions => _subscriptions.Register;
    public Event<RenderOperation> Dead => _onDead.Event;

    protected RenderOperation(Screen screen, Own<RenderPipeline> pipelineOwn)
    {
        _screen = screen;
        _pipelineOwn = pipelineOwn;
        _lifeState = LifeState.New;
        screen.RenderOperations.Add(this);
    }

    internal abstract void FrameInit();

    internal Own<RenderPass> GetRenderPass(in CommandEncoder encoder) => CreateRenderPass(encoder);

    protected virtual Own<RenderPass> CreateRenderPass(in CommandEncoder encoder)
    {
        return RenderPass.SurfaceRenderPass(in encoder);
    }

    protected abstract void Render(RenderPass renderPass);

    internal abstract void FrameEnd();

    internal void InvokeRender(RenderPass renderPass) => Render(renderPass);

    internal void Release()
    {
        _pipelineOwn.Dispose();
        _onDead.Invoke(this);
        _subscriptions.Dispose();
    }

    public bool Terminate()
    {
        var currentState = InterlockedEx.CompareExchange(ref _lifeState, LifeState.Terminating, LifeState.Alive);
        if(currentState != LifeState.Alive) {
            return false;
        }
        Screen.RenderOperations.Remove(this);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetLifeStateAlive()
    {
        Debug.Assert(_lifeState == LifeState.New);
        _lifeState = LifeState.Alive;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetLifeStateDead()
    {
        Debug.Assert(_lifeState == LifeState.Terminating);
        _lifeState = LifeState.Dead;
    }
}

public abstract class RenderOperation<TShader, TMaterial>
    : RenderOperation
    where TShader : Shader<TShader, TMaterial>
    where TMaterial : Material<TMaterial, TShader>
{
    private readonly Own<TShader> _shaderOwn;

    public TShader Shader => _shaderOwn.AsValue();

    protected RenderOperation(Own<TShader> shaderOwn, Own<RenderPipeline> pipelineOwn) : base(shaderOwn.AsValue().Screen, pipelineOwn)
    {
        shaderOwn.ThrowArgumentExceptionIfNone();
        _shaderOwn = shaderOwn;
    }
}

public abstract class ObjectLayer<TSelf, TVertex, TShader, TMaterial>
    : RenderOperation<TShader, TMaterial>
    where TSelf : ObjectLayer<TSelf, TVertex, TShader, TMaterial>
    where TVertex : unmanaged, IVertex<TVertex>
    where TShader : Shader<TShader, TMaterial>
    where TMaterial : Material<TMaterial, TShader>
{
    private readonly List<FrameObject<TSelf, TVertex, TShader, TMaterial>> _list;
    private readonly List<FrameObject<TSelf, TVertex, TShader, TMaterial>> _addedList;
    private readonly List<FrameObject<TSelf, TVertex, TShader, TMaterial>> _removedList;
    private readonly object _sync = new object();

    protected ObjectLayer(Own<TShader> shader, Func<TShader, RenderPipelineDescriptor> pipelineDescGen)
        : this(shader, pipelineDescGen(shader.AsValue())) { }

    protected ObjectLayer(Own<TShader> shader, Func<TShader, Own<RenderPipeline>> pipelineGen)
        : this(shader, pipelineGen(shader.AsValue())) { }

    protected ObjectLayer(Own<TShader> shader, in RenderPipelineDescriptor pipelineDesc)
        : this(shader, RenderPipeline.Create(shader.AsValue().Screen, pipelineDesc)) { }

    protected ObjectLayer(Own<TShader> shader, Own<RenderPipeline> pipeline) : base(shader, pipeline)
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

    internal override void FrameInit()
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

    internal override void FrameEnd()
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

    protected override void Render(RenderPass renderPass)
    {
        renderPass.SetPipeline(Pipeline);
        foreach(var obj in _list.AsSpan()) {
            if(obj is Renderable<TSelf, TVertex, TShader, TMaterial> renderable) {
                renderable.Render(renderPass);
            }
        }
    }
}
