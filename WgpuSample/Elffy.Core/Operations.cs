#nullable enable
using Elffy.Effective;
using Elffy.NativeBind;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Elffy;

public sealed class Operations
{
    // [NOTE]
    // The order of the elements in the list is not guaranteed.

    private readonly Screen _screen;
    private readonly List<Operation> _list;
    private readonly List<Operation> _addedList;
    private readonly List<Operation> _removedList;
    private EventSource<Operations> _added;
    private EventSource<Operations> _removed;
    private readonly object _sync = new object();

    public Screen Screen => _screen;

    public Event<Operations> Added => _added.Event;
    public Event<Operations> Removed => _removed.Event;

    internal Operations(Screen screen)
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

    internal void EarlyUpdate()
    {
        foreach(var op in _list.AsSpan()) {
            op.InvokeEarlyUpdate();
        }
    }

    internal void Update()
    {
        foreach(var op in _list.AsSpan()) {
            op.InvokeUpdate();
        }
    }

    internal void LateUpdate()
    {
        foreach(var op in _list.AsSpan()) {
            op.InvokeLateUpdate();
        }
    }

    internal void Execute(Rust.Ref<Wgpu.TextureView> surfaceView)
    {
        var screen = _screen;
        var lights = screen.Lights;
        lights.UpdateLightMatrix();
        {
            var context = new RenderShadowMapContext(lights);
            foreach(var op in _list.AsSpan()) {
                op.InvokeRenderShadowMap(in context);
            }

            //if(screen.FrameNum == 1000) {
            //    var texture = context.ShadowMap.Texture;
            //    texture.ReadCallback(data =>
            //    {
            //        Debug.WriteLine(data.Length);
            //        var pixels = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, float>(data);
            //        using var image = new Elffy.Imaging.Image((int)texture.Width, (int)texture.Height, false);
            //        var dest = image.GetPixels();
            //        for(int i = 0; i < pixels.Length; i++) {
            //            var p = (byte)float.Clamp(pixels[i] * 255, 0, 255);
            //            p = (byte)(255 - p);
            //            dest[i] = new ColorByte(p, p, p, 255);
            //        }
            //        Elffy.Imaging.ImageExtensions.SaveAsPng(image, "shadow.png");
            //    });
            //}
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

    internal void Remove(Operation operation)
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
                        removedItem.InvokeRelease();
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
