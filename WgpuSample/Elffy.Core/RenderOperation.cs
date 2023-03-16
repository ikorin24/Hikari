#nullable enable
using Elffy.Effective;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy;

public abstract class RenderOperation
{
    private readonly IHostScreen _screen;
    private readonly Own<RenderPipeline> _pipelineOwn;
    private readonly Own<Shader> _shaderOwn;
    private LifeState _lifeState;

    public IHostScreen Screen => _screen;
    public Shader Shader => _shaderOwn.AsValue();
    public RenderPipeline Pipeline => _pipelineOwn.AsValue();
    public LifeState LifeState => _lifeState;

    protected RenderOperation(Own<Shader> shaderOwn, Own<RenderPipeline> pipelineOwn)
    {
        shaderOwn.ThrowArgumentExceptionIfNone();
        _screen = shaderOwn.AsValue().Screen;
        _shaderOwn = shaderOwn;
        _pipelineOwn = pipelineOwn;
        _lifeState = LifeState.New;
    }

    internal void Release()
    {
        _pipelineOwn.Dispose();
    }

    internal void Render(RenderPass renderPass)
    {
        renderPass.SetPipeline(Pipeline);
    }

    public bool Terminate()
    {
        var currentState = InterlockedEx.CompareExchange(ref _lifeState, LifeState.Terminating, LifeState.Alive);
        if(currentState != LifeState.Alive) {
            return false;
        }
        _screen.RenderOperations.Remove(this);
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

public sealed class ObjectLayer : RenderOperation
{
    private readonly List<FrameObject> _list;
    private readonly List<FrameObject> _addedList;
    private readonly List<FrameObject> _removedList;
    private readonly object _sync = new object();

    private ObjectLayer(Own<Shader> shaderOwn, Own<RenderPipeline> pipelineOwn) : base(shaderOwn, pipelineOwn)
    {
        _list = new List<FrameObject>();
        _addedList = new List<FrameObject>();
        _removedList = new List<FrameObject>();
    }

    public static ObjectLayer Create(Own<Shader> shader, in RenderPipelineDescriptor pipelineDesc)
    {
        shader.ThrowArgumentExceptionIfNone();
        var screen = shader.AsValue().Screen;
        var pipeline = RenderPipeline.Create(screen, in pipelineDesc);
        var self = new ObjectLayer(shader, pipeline);
        screen.RenderOperations.Add(self);
        return self;
    }

    internal void Add(FrameObject frameObject)
    {
        lock(_sync) {
            _addedList.Add(frameObject);
        }
    }

    internal void ApplyAdd()
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

    internal void Remove(FrameObject frameObject)
    {
        lock(_sync) {
            Debug.Assert(frameObject.LifeState == LifeState.Terminating);
            _removedList.Add(frameObject);
        }
    }

    internal void ApplyRemove()
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
}
