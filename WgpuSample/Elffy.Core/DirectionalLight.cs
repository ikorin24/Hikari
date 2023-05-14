#nullable enable
using System;
using System.Runtime.InteropServices;

namespace Elffy;

public sealed class DirectionalLight : IScreenManaged
{
    private readonly Screen _screen;
    private readonly Own<Buffer> _buffer;
    private DirectionalLightData _data;
    private Own<Buffer> _lightDepth;
    private Own<BindGroup> _lightDepthBindGroup;
    private Own<BindGroupLayout> _lightDepthBindGroupLayout;
    private readonly Vector2u _shadowMapSize;

    private readonly object _sync = new();

    public Screen Screen => _screen;

    public bool IsManaged => _buffer.IsNone == false;

    public void Validate() => IScreenManaged.DefaultValidate(this);

    public BindGroup LightDepthBindGroup => _lightDepthBindGroup.AsValue();
    public BindGroupLayout LightDepthBindGroupLayout => _lightDepthBindGroupLayout.AsValue();

    public Vector2u ShadowMapSize => _shadowMapSize;

    public Vector3 Direction
    {
        get
        {
            lock(_sync) {
                return _data.Direction;
            }
        }
    }

    public Color3 Color
    {
        get
        {
            lock(_sync) {
                return _data.Color;
            }
        }
    }

    public BufferSlice<byte> DataBuffer => _buffer.AsValue().Slice();

    public void SetLightData(in Vector3 direction, in Color3 color)
    {
        direction.Normalize();
        if(direction.ContainsNaNOrInfinity) {
            throw new ArgumentException(nameof(direction));
        }
        var buffer = _buffer.AsValue();
        lock(_sync) {
            _data = new DirectionalLightData(direction, color);
            buffer.Write(0, _data);
        }
    }

    internal DirectionalLight(Screen screen)
    {
        var data = new DirectionalLightData(
            new Vector3(0f, -1f, -0.3f).Normalized(),
            new Color3(1, 1, 1)
        );
        _screen = screen;
        _buffer = Buffer.Create(screen, in data, BufferUsages.Storage | BufferUsages.CopyDst);
        _data = data;
        CreateLightDepth(screen, out _lightDepth, out _lightDepthBindGroup, out _lightDepthBindGroupLayout, out _shadowMapSize);
    }

    private static unsafe void CreateLightDepth(Screen screen, out Own<Buffer> depth, out Own<BindGroup> bindGroup, out Own<BindGroupLayout> bindGroupLayout, out Vector2u shadowMapSize)
    {
        const u32 Width = 1024;
        const u32 Height = 1024;
        shadowMapSize = new Vector2u(Width, Height);

        nuint len = (nuint)Width * Height * sizeof(f32) + (nuint)sizeof(Vector2u);

        var mem = (byte*)NativeMemory.AllocZeroed(len);
        *(Vector2u*)mem = new Vector2u(Width, Height);
        try {
            depth = Buffer.Create(screen, mem, len, BufferUsages.Storage);
            bindGroup = BindGroup.Create(screen, new()
            {
                Layout = BindGroupLayout.Create(screen, new()
                {
                    Entries = new BindGroupLayoutEntry[]
                    {
                        BindGroupLayoutEntry.Buffer(0, ShaderStages.Compute, new() { Type = BufferBindingType.Storate }),
                    },
                }).AsValue(out bindGroupLayout),
                Entries = new[]
                {
                    BindGroupEntry.Buffer(0, depth.AsValue()),
                },
            });
        }
        finally {
            NativeMemory.Free(mem);
        }
    }

    internal void DisposeInternal()
    {
        _buffer.Dispose();
        _lightDepth.Dispose();
        _lightDepthBindGroup.Dispose();
        _lightDepthBindGroupLayout.Dispose();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    private readonly struct DirectionalLightData
    {
        private readonly Vector4 _direction;    // (x, y, z, _)
        private readonly Color4 _color;        // (r, g, b, _)

        public Vector3 Direction => _direction.Xyz;
        public Color3 Color => _color.ToColor3();

        public DirectionalLightData(in Vector3 direction, in Color3 color)
        {
            _direction = new Vector4(direction, 0);
            _color = new Color4(color, 0);
        }
    }
}
