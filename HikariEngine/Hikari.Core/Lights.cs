﻿#nullable enable
using System.Runtime.InteropServices;

namespace Hikari;

public sealed partial class Lights
{
    private readonly Screen _screen;
    private readonly DirectionalLight _dirLight;
    private readonly CachedOwnBuffer<BufferData> _ambientLightData;
    private readonly Own<BindGroupLayout> _bindGroupLayout;
    private readonly Own<BindGroup> _bindGroup;

    public Screen Screen => _screen;

    public DirectionalLight DirectionalLight => _dirLight;

    public float AmbientStrength
    {
        get => _ambientLightData.Data.AmbientStrength;
        set
        {
            _ambientLightData.WriteData(new BufferData
            {
                AmbientStrength = value,
            });
        }
    }

    public BindGroup DataBindGroup => _bindGroup.AsValue();

    internal Lights(Screen screen)
    {
        _screen = screen;
        _dirLight = new DirectionalLight(screen);
        _ambientLightData = new(screen, new BufferData
        {
            AmbientStrength = 0.06f,
        }, BufferUsages.Storage | BufferUsages.CopyDst);
        _bindGroupLayout = BindGroupLayout.Create(screen, new BindGroupLayoutDescriptor
        {
            Entries =
            [
                BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex | ShaderStages.Fragment | ShaderStages.Compute, new()
                {
                    Type = BufferBindingType.StorageReadOnly,
                }),
                BindGroupLayoutEntry.Buffer(1, ShaderStages.Vertex | ShaderStages.Fragment | ShaderStages.Compute, new()
                {
                    Type = BufferBindingType.StorageReadOnly,
                }),
            ],
        });
        _bindGroup = BindGroup.Create(screen, new BindGroupDescriptor
        {
            Layout = _bindGroupLayout.AsValue(),
            Entries =
            [
                BindGroupEntry.Buffer(0, _dirLight.DataBuffer),
                BindGroupEntry.Buffer(1, _ambientLightData),
            ],
        });
    }

    internal void DisposeInternal()
    {
        _dirLight.DisposeInternal();
        _ambientLightData.Dispose();
        _bindGroupLayout.Dispose();
        _bindGroup.Dispose();
    }

    [BufferDataStruct]
    private partial record struct BufferData
    {
        [FieldOffset(OffsetOf.AmbientStrength)]
        public float AmbientStrength;
    }
}
