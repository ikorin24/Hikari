#nullable enable
using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Hikari;

public sealed partial class Renderer : IRenderer
{
    private readonly Screen _screen;
    private readonly Mesh _mesh;
    private readonly ImmutableArray<IMaterial> _materials;
    private readonly TypedOwnBuffer<ModelUniformValue> _modelDataBuffer;
    private readonly Own<BindGroupLayout> _modelDataBindGroupLayout;
    private readonly Own<BindGroup> _modelDataBindGroup;
    private bool _isVisible = true;
    private bool _areAllAncestorsVisible = true;

    public Screen Screen => _screen;

    public Mesh Mesh => _mesh;

    public int SubrendererCount => _materials.Length;

    public BindGroup ModelDataBindGroup => _modelDataBindGroup.AsValue();

    public bool IsVisible
    {
        get => _isVisible;
        set => _isVisible = value;
    }

    public bool IsVisibleInHierarchy => _isVisible && _areAllAncestorsVisible;

    bool IRenderer.AreAllAncestorsVisible
    {
        get => _areAllAncestorsVisible;
        set => _areAllAncestorsVisible = value;
    }

    internal Renderer(Mesh mesh, ImmutableArray<IMaterial> materials)
    {
        if(mesh.Submeshes.Length != materials.Length) {
            ThrowHelper.ThrowArgument(nameof(materials), "The number of materials does not equal the number of submeshes");
        }
        var screen = mesh.Screen;
        _screen = screen;
        _mesh = mesh;
        _materials = materials;

        var modelDataBuffer = new TypedOwnBuffer<ModelUniformValue>(screen, default, BufferUsages.Uniform | BufferUsages.CopyDst | BufferUsages.Storage);
        var bgl = BindGroupLayout.Create(screen, new()
        {
            Entries =
            [
                BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex, new() { Type = BufferBindingType.Uniform }),
            ],
        });
        _modelDataBindGroupLayout = bgl;
        _modelDataBindGroup = BindGroup.Create(screen, new()
        {
            Layout = bgl.AsValue(),
            Entries =
            [
                BindGroupEntry.Buffer(0, modelDataBuffer),
            ],
        });
        _modelDataBuffer = modelDataBuffer;
    }

    internal void PrepareForRender(FrameObject obj)
    {
        var model = obj.GetModel(out var isUniformScale);
        _modelDataBuffer.WriteData(new()
        {
            Model = model,
            IsUniformScale = isUniformScale ? 1 : 0,
        });

        foreach(var material in _materials) {
            material.Shader.PrepareForRender(obj, material);
        }
    }

    public IMaterial GetMaterial(int submeshIndex)
    {
        return _materials[submeshIndex];
    }

    public T GetMaterial<T>(int submeshIndex) where T : IMaterial
    {
        var material = _materials[submeshIndex];
        return (T)material;
    }

    internal Subrenderers GetSubrenderers() => new Subrenderers(this);

    void IRenderer.DisposeInternal() => DisposeInternal();

    internal void DisposeInternal()
    {
        _modelDataBuffer.Dispose();
        _modelDataBindGroupLayout.Dispose();
        _modelDataBindGroup.Dispose();
    }

    internal readonly record struct Subrenderers
    {
        private readonly Renderer _renderer;
        public Subrenderers(Renderer renderer) => _renderer = renderer;
        public readonly SubrendererEnumerator GetEnumerator() => new(_renderer);
    }

    internal ref struct SubrendererEnumerator
    {
        private readonly Mesh _mesh;
        private readonly ReadOnlySpan<SubmeshData> _submeshes;
        private readonly ReadOnlySpan<IMaterial> _materials;
        private int _index;

        public readonly Subrenderer Current => new Subrenderer(_materials[_index], _mesh, _submeshes[_index]);

        public SubrendererEnumerator(Renderer renderer)
        {
            _mesh = renderer.Mesh;
            _submeshes = _mesh.Submeshes;
            _materials = renderer._materials.AsSpan();
            _index = -1;
        }

        public bool MoveNext()
        {
            return ++_index < _materials.Length;
        }
    }

    [BufferDataStruct]
    internal partial struct ModelUniformValue
    {
        [FieldOffset(OffsetOf.Model)]
        public required Matrix4 Model;
        [FieldOffset(OffsetOf.IsUniformScale)]
        public required int IsUniformScale;  // true: 1, false: 0
    }
}

internal sealed class NoneRenderer : IRenderer
{
    private readonly Screen _screen;
    private bool _isVisible = true;
    private bool _areAllAncestorsVisible = true;

    public Screen Screen => _screen;

    public bool IsVisible
    {
        get => _isVisible;
        set => _isVisible = value;
    }
    public bool AreAllAncestorsVisible
    {
        get => _areAllAncestorsVisible;
        set => _areAllAncestorsVisible = value;
    }
    public bool IsVisibleInHierarchy => _isVisible && _areAllAncestorsVisible;

    public NoneRenderer(Screen screen)
    {
        _screen = screen;
    }

    public void DisposeInternal()
    {
    }
}

internal interface IRenderer
{
    Screen Screen { get; }
    bool IsVisible { get; set; }
    bool IsVisibleInHierarchy { get; }
    bool AreAllAncestorsVisible { get; set; }
    void DisposeInternal();
}

internal readonly record struct Subrenderer
{
    public IMaterial Material { get; }
    public Mesh Mesh { get; }
    public SubmeshData Submesh { get; }

    public Subrenderer(IMaterial material, Mesh mesh, SubmeshData submesh)
    {
        Material = material;
        Mesh = mesh;
        Submesh = submesh;
    }

    public void Deconstruct(out IMaterial material, out Mesh mesh, out SubmeshData submesh)
    {
        material = Material;
        mesh = Mesh;
        submesh = Submesh;
    }
}
