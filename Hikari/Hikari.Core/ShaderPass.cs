#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hikari;

//[Obsolete]
//public sealed class ShaderPass
//{
//    private readonly int _passIndex;
//    private readonly RenderPipeline _pipeline;
//    private readonly PipelineLayout _layout;
//    private readonly ShaderModule _module;
//    private readonly int _sortOrder;
//    private readonly RenderPassFactory _renderPassFactory;
//    private readonly Shader _shader;
//    private readonly List<FrameObject> _list;

//    public Screen Screen => _pipeline.Screen;
//    public RenderPipeline Pipeline => _pipeline;
//    public PipelineLayout Layout => _layout;
//    public ShaderModule Module => _module;
//    public Shader Shader => _shader;
//    public int Index => _passIndex;
//    public int SortOrder => _sortOrder;

//    internal ShaderPass(int passIndex, in ShaderPassDescriptor info, Shader shader, RenderPipeline pipeline, PipelineLayout layout, ShaderModule module)
//    {
//        _passIndex = passIndex;
//        _shader = shader;
//        _pipeline = pipeline;
//        _layout = layout;
//        _module = module;
//        _sortOrder = info.SortOrder;
//        _renderPassFactory = info.RenderPassFactory;
//        _list = new();
//    }

//    internal void Register<TSelf, TVertex, TShader, TMaterial>(FrameObject<TSelf, TVertex, TShader, TMaterial> frameObject)
//        where TSelf : FrameObject<TSelf, TVertex, TShader, TMaterial>
//        where TVertex : unmanaged, IVertex
//        where TShader : Shader<TShader, TMaterial>
//        where TMaterial : Material<TMaterial, TShader>
//    {
//        Debug.Assert(frameObject.LifeState == LifeState.New);
//        frameObject.Alive.Subscribe(self => _list.Add(self));
//        frameObject.Dead.Subscribe(self => _list.SwapRemove(self));
//    }

//    internal void Execute()
//    {
//        Debug.Assert(Screen.MainThread.IsCurrentThread);
//        using(var passOwn = _renderPassFactory.Invoke(Screen)) {
//            var pass = passOwn.AsValue();
//            pass.SetPipeline(Pipeline);
//            foreach(var frameObject in _list.AsSpan()) {
//                frameObject.OnRender(pass, this);
//            }
//        }
//    }
//}
