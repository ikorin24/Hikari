#nullable enable
using Elffy.Effective;
using System;
using System.Collections.Generic;

namespace Elffy;

public sealed class RenderOperation
{
    private readonly IHostScreen _screen;
    private readonly Own<RenderPipeline> _pipelineOwn;
    private readonly Own<Shader> _shaderOwn;

    public IHostScreen Screen => _screen;
    public Shader Shader => _shaderOwn.AsValue();
    public RenderPipeline Pipeline => _pipelineOwn.AsValue();

    private RenderOperation(IHostScreen screen, Own<Shader> shaderOwn, Own<RenderPipeline> pipelineOwn)
    {
        _screen = screen;
        _shaderOwn = shaderOwn;
        _pipelineOwn = pipelineOwn;
    }

    private void Release()
    {
        _pipelineOwn.Dispose();
    }

    public static RenderOperation Create(Own<Shader> shader, in RenderPipelineDescriptor pipelineDesc)
    {
        shader.ThrowArgumentExceptionIfNone();
        var screen = shader.AsValue().Screen;
        var pipeline = RenderPipeline.Create(screen, in pipelineDesc);
        var self = new RenderOperation(screen, shader, pipeline);
        var selfOwn = new Own<RenderOperation>(self, static self => self.Release());
        return screen.RenderOperations.Add(selfOwn);
    }

    internal void Render(RenderPass renderPass)
    {
        renderPass.SetPipeline(Pipeline);
    }

    public void Terminate()
    {
        _screen.RenderOperations.Remove(this);
    }
}

public sealed class RenderOperations
{
    private readonly IHostScreen _screen;
    private readonly List<Own<RenderOperation>> _list;
    private readonly List<(Own<RenderOperation>, Action<RenderOperation>?)> _addedList;
    private readonly List<(RenderOperation, Action<RenderOperation>?)> _removedList;
    private EventSource<RenderOperations> _added;
    private EventSource<RenderOperations> _removed;

    public IHostScreen Screen => _screen;

    internal RenderOperations(IHostScreen screen)
    {
        _screen = screen;
        _list = new();
        _addedList = new();
        _removedList = new();
    }

    [Obsolete("make this method internal")]
    public void Render(RenderPass renderPass)
    {
        foreach(var op in _list.AsSpan()) {
            op.AsValue().Render(renderPass);
        }
    }

    internal RenderOperation Add(Own<RenderOperation> operation)
    {
        operation.ThrowArgumentExceptionIfNone();
        _addedList.Add((operation, null));
        return operation.AsValue();
    }

    internal void ApplyAdd()
    {
        var addedList = _addedList;
        if(addedList.Count == 0) {
            return;
        }

        int addedCount;
        {
            var addedListSpan = addedList.AsSpan();
            addedCount = addedListSpan.Length;
            var list = _list;
            foreach(var (item, onAdded) in addedListSpan) {
                list.Add(item);
                onAdded?.Invoke(item.AsValue());
            }
        }
        if(addedCount == addedList.Count) {
            addedList.Clear();
        }
        else {
            addedList.RemoveRange(0, addedCount);
        }
        _added.Invoke(this);
    }

    internal void Remove(RenderOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        _removedList.Add((operation, null));
    }

    internal void ApplyRemove()
    {
        var removedList = _removedList;
        if(removedList.Count == 0) {
            return;
        }
        int removedCount;
        {
            var removedListSpan = removedList.AsSpan();
            removedCount = removedListSpan.Length;
            var list = _list;
            foreach(var (item, onRemove) in removedListSpan) {

                int i = 0;
                foreach(var owned in list.AsSpan()) {
                    if(owned.AsValue() == item) { break; }
                    i++;
                }
                list.RemoveAt(i);
                onRemove?.Invoke(item);
            }
        }
        if(removedCount == removedList.Count) {
            removedList.Clear();
        }
        else {
            removedList.RemoveRange(0, removedCount);
        }
        _removed.Invoke(this);
    }
}
