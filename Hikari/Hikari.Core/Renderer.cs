#nullable enable
using System;
using System.Collections.Immutable;

namespace Hikari;

public sealed class Renderer
{
    private readonly Screen _screen;
    private readonly MaybeOwn<Mesh> _mesh;
    private readonly ImmutableArray<Own<Material>> _materials;

    public Screen Screen => _screen;

    public Mesh Mesh => _mesh.AsValue();

    public int SubrendererCount => _materials.Length;

    internal Renderer(MaybeOwn<Mesh> mesh, ImmutableArray<Own<Material>> materials)
    {
        var meshValue = mesh.AsValue();
        if(meshValue.Submeshes.Length != materials.Length) {
            ThrowHelper.ThrowArgument(nameof(materials), "The number of materials does not equal the number of submeshes");
        }
        _screen = meshValue.Screen;
        _mesh = mesh;
        _materials = materials;
    }

    public Material GetMaterial(int submeshIndex)
    {
        return _materials[submeshIndex].AsValue();
    }

    public T GetMaterial<T>(int submeshIndex) where T : Material
    {
        var material = _materials[submeshIndex].AsValue();
        return (T)material;
    }

    internal Subrenderers GetSubrenderers() => new Subrenderers(this);

    internal void DisposeInternal()
    {
        _mesh.Dispose();
        foreach(var material in _materials) {
            material.Dispose();
        }
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
        private readonly ReadOnlySpan<Own<Material>> _materials;
        private int _index;

        public readonly Subrenderer Current => new Subrenderer(_materials[_index].AsValue(), _mesh, _submeshes[_index]);

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
}

internal readonly record struct Subrenderer
{
    public Material Material { get; }
    public Mesh Mesh { get; }
    public SubmeshData Submesh { get; }

    public Subrenderer(Material material, Mesh mesh, SubmeshData submesh)
    {
        Material = material;
        Mesh = mesh;
        Submesh = submesh;
    }

    public void Deconstruct(out Material material, out Mesh mesh, out SubmeshData submesh)
    {
        material = Material;
        mesh = Mesh;
        submesh = Submesh;
    }
}
