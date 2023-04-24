#nullable enable
using System;
using System.Runtime.InteropServices;

namespace Elffy;

public sealed class DirectionalLight
{
    private readonly Own<Buffer> _buffer;
    private DirectionalLightData _data;
    private readonly object _sync = new();

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
        _buffer = Buffer.Create(screen, in data, BufferUsages.Storage | BufferUsages.CopyDst);
        _data = data;
    }

    internal void DisposeInternal()
    {
        _buffer.Dispose();
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
