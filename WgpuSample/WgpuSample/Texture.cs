#nullable enable
using Elffy.Bind;
using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Elffy;

public sealed class Texture : IEngineManaged, IDisposable
{
    private IHostScreen? _screen;
    private Box<Wgpu.Texture> _native;
    private CE.TextureDescriptor _desc;
    private Wgpu.Extent3d _size;

    public IHostScreen? Screen => _screen;

    internal Ref<Wgpu.Texture> NativeRef => _native.AsRefChecked();
    internal MutRef<Wgpu.Texture> NativeMut => _native.AsMutChecked();

    public bool IsEmpty => _native.IsInvalid;

    public int Width => (int)_size.width;

    public int Height => (int)_size.height;

    public Vector2i Size => new Vector2i(Width, Height);

    public Texture()
    {
    }

    public TextureView CreateTextureView()
    {
        _native.ThrowIfInvalid();
        return new TextureView(this);
    }

    public unsafe void LoadFile(IHostScreen screen, string filepath)  // TODO: remove
    {
        ArgumentNullException.ThrowIfNull(screen);
        var screenRef = screen.Ref.AsRef();

        var (pixelBytes, width, height) = SamplePrimitives.LoadImagePixels(filepath);
        var size = new Wgpu.Extent3d
        {
            width = width,
            height = height,
            depth_or_array_layers = 1,
        };
        var desc = new CE.TextureDescriptor()
        {
            dimension = CE.TextureDimension.D2,
            format = Wgpu.TextureFormat.Rgba8UnormSrgb,
            mip_level_count = 1,
            sample_count = 1,
            size = size,
            usage = Wgpu.TextureUsages.TEXTURE_BINDING | Wgpu.TextureUsages.COPY_DST,
        };
        var texture = screenRef.CreateTexture(desc);
        fixed(byte* p = pixelBytes) {
            screenRef.WriteTexture(
                new CE.ImageCopyTexture
                {
                    texture = texture,
                    mip_level = 0,
                    aspect = CE.TextureAspect.All,
                    origin_x = 0,
                    origin_y = 0,
                    origin_z = 0,
                },
                new Slice<byte>(p, pixelBytes.Length),
                new Wgpu.ImageDataLayout
                {
                    offset = 0,
                    bytes_per_row = 4 * width,
                    rows_per_image = height,
                },
                size);
        }

        _screen = screen;
        _size = size;
        _desc = desc;
        _native = texture;
    }

    public void Dispose()
    {
        if(_native.IsInvalid) {
            return;
        }
        _native.DestroyTexture();
        _native = Box<Wgpu.Texture>.Invalid;
        _screen = null;
    }
}

public sealed class TextureView : IEngineManaged, IDisposable
{
    private IHostScreen? _screen;
    private Box<Wgpu.TextureView> _native;
    private Texture? _texture;

    public IHostScreen? Screen => _screen;

    public TextureView(Texture texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        texture.ThrowIfNotEngineManaged();

        _texture = texture;
        _native = texture.NativeRef.CreateTextureView();
    }

    public void Dispose()
    {
        if(_native.IsInvalid) {
            return;
        }
        _texture = null;
        _native.DestroyTextureView();
        _native = Box<Wgpu.TextureView>.Invalid;
        _screen = null;
    }
}


public sealed class Sampler : IEngineManaged, IDisposable
{
    private IHostScreen? _screen;
    private Box<Wgpu.Sampler> _native;

    public IHostScreen? Screen => _screen;

    public Sampler(IHostScreen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var screenRef = screen.AsRef();
        var desc = new CE.SamplerDescriptor
        {
            address_mode_u = Wgpu.AddressMode.ClampToEdge,
            address_mode_v = Wgpu.AddressMode.ClampToEdge,
            address_mode_w = Wgpu.AddressMode.ClampToEdge,
            mag_filter = Wgpu.FilterMode.Linear,
            min_filter = Wgpu.FilterMode.Nearest,
            mipmap_filter = Wgpu.FilterMode.Nearest,
            anisotropy_clamp = 0,
            lod_max_clamp = 0,
            lod_min_clamp = 0,
            border_color = Opt<CE.SamplerBorderColor>.None,
            compare = Opt<Wgpu.CompareFunction>.None,
        };
        _native = screenRef.CreateSampler(desc);
        _screen = screen;
    }

    public void Dispose()
    {
        if(_native.IsInvalid) {
            return;
        }
        _native.DestroySampler();
        _native = Box<Wgpu.Sampler>.Invalid;
        _screen = null;
    }
}

public sealed class Shader : IEngineManaged, IDisposable
{
    private IHostScreen? _screen;
    private Box<Wgpu.ShaderModule> _native;

    public IHostScreen? Screen => _screen;

    public Shader(IHostScreen screen, ReadOnlySpan<byte> shaderSource)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var shader = screen.AsRef().CreateShaderModule(shaderSource);
        _screen = screen;
        _native = shader;
    }

    public void Dispose()
    {
        if(_native.IsInvalid) {
            return;
        }
        _native.DestroyShaderModule();
        _native = Box<Wgpu.ShaderModule>.Invalid;
        _screen = null;
    }
}

public sealed class Buffer : IEngineManaged, IDisposable
{
    private IHostScreen? _screen;
    private Box<Wgpu.Buffer> _native;
    private BufferUsages _usage;

    public IHostScreen? Screen => _screen;

    public BufferUsages Usage => _usage;

    private Buffer(IHostScreen screen, Box<Wgpu.Buffer> native, BufferUsages usage)
    {
        _screen = screen;
        _native = native;
        _usage = usage;
    }

    public static Buffer CreateUniformBuffer<T>(IHostScreen screen, ReadOnlySpan<T> data) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromSpan(screen, data, BufferUsages.Uniform | BufferUsages.CopyDst);
    }

    public static Buffer CreateVertexBuffer<T>(IHostScreen screen, ReadOnlySpan<T> data) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromSpan(screen, data, BufferUsages.Vertex | BufferUsages.CopyDst);
    }

    public static Buffer CreateIndexBuffer<T>(IHostScreen screen, ReadOnlySpan<T> data) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromSpan(screen, data, BufferUsages.Index | BufferUsages.CopyDst);
    }

    public unsafe static Buffer Create<T>(IHostScreen screen, ReadOnlySpan<byte> data, BufferUsages usage) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromSpan(screen, data, usage);
    }

    public unsafe static Buffer Create(IHostScreen screen, byte* ptr, nuint byteLength, BufferUsages usage)
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromPtr(screen, ptr, byteLength, usage);
    }

    private unsafe static Buffer CreateFromSpan<T>(IHostScreen screen, ReadOnlySpan<T> data, BufferUsages usage) where T : unmanaged
    {
        fixed(T* ptr = data) {
            var bytelen = (nuint)data.Length * (nuint)sizeof(T);
            return CreateFromPtr(screen, (byte*)ptr, bytelen, usage);
        }
    }

    private unsafe static Buffer CreateFromPtr(IHostScreen screen, byte* ptr, nuint byteLength, BufferUsages usage)
    {
        var screenRef = screen.AsRef();
        usage.TryMapTo(out Wgpu.BufferUsages nativeUsage).WithDebugAssertTrue();
        var data = new Slice<u8>(ptr, byteLength);
        var buffer = screenRef.CreateBufferInit(data, nativeUsage);
        return new Buffer(screen, buffer, usage);
    }

    public void Dispose()
    {
        if(_native.IsInvalid) {
            return;
        }
        _native.DestroyBuffer();
        _native = Box<Wgpu.Buffer>.Invalid;
        _screen = null;
    }
}

public sealed class BindGroupLayout : IEngineManaged, IDisposable
{
    private IHostScreen? _screen;
    private Box<Wgpu.BindGroupLayout> _native;

    public IHostScreen? Screen => _screen;

    private BindGroupLayout(IHostScreen screen, Box<Wgpu.BindGroupLayout> native)
    {
        _screen = screen;
        _native = native;
    }

    public unsafe static BindGroupLayout Create(IHostScreen screen, int entryCount, BuildAction action)
    {
        ArgumentNullException.ThrowIfNull(screen);
        ArgumentNullException.ThrowIfNull(action);
        if(entryCount <= 0) {
            throw new ArgumentOutOfRangeException(nameof(entryCount));
        }

        BindGroupLayout bindGroupLayout;
        if(entryCount <= 16) {
            Span<CE.BindGroupLayoutEntry> entries = stackalloc CE.BindGroupLayoutEntry[entryCount];
            var builder = new Builder(screen, entries);
            bindGroupLayout = action.Invoke(ref builder);
        }
        else {
            var bytelen = entryCount * sizeof(CE.BindGroupLayoutEntry);
            var array = ArrayPool<byte>.Shared.Rent(bytelen);
            try {
                var entries = MemoryMarshal.Cast<byte, CE.BindGroupLayoutEntry>(array.AsSpan(0, bytelen));
                var builder = new Builder(screen, entries);
                bindGroupLayout = action.Invoke(ref builder);
            }
            finally {
                ArrayPool<byte>.Shared.Return(array);
            }
        }
        return bindGroupLayout ?? throw new ArgumentException("the builder returns null");
    }

    public void Dispose()
    {
        if(_native.IsInvalid) {
            return;
        }
        _native.DestroyBindGroupLayout();
        _native = Box<Wgpu.BindGroupLayout>.Invalid;
        _screen = null;
    }

    public delegate BindGroupLayout BuildAction(ref Builder builder);

    public unsafe ref struct Builder
    {
        private readonly IHostScreen _screen;
        private Span<CE.BindGroupLayoutEntry> _entries;
        private int _index;

        internal Builder(IHostScreen screen, Span<CE.BindGroupLayoutEntry> entries)
        {
            _screen = screen;
            _entries = entries;
            _index = 0;
        }

        public BindGroupLayout Build()
        {
            var screen = _screen;
            var entryCount = _entries.Length;
            fixed(CE.BindGroupLayoutEntry* ptr = _entries) {
                var entries = new Slice<CE.BindGroupLayoutEntry>(ptr, entryCount);
                var desc = new CE.BindGroupLayoutDescriptor
                {
                    entries = new Slice<CE.BindGroupLayoutEntry>(ptr, entryCount),
                };
                var bindGroupLayout = screen.AsRef().CreateBindGroupLayout(desc);
                return new BindGroupLayout(screen, bindGroupLayout);
            }
        }

        public void SetTextureEntry(
            u32 binding,
            ShaderStages visibility,
            bool multisampled = false,
            TextureViewDimension viewDimension = TextureViewDimension.D2,
            TextureSampleType sampleType = TextureSampleType.FloatFilterable)
        {
            visibility.TryMapTo(out Wgpu.ShaderStages visibilityNative).WithDebugAssertTrue();
            viewDimension.TryMapTo(out CE.TextureViewDimension viewDimensionNative).WithDebugAssertTrue();
            sampleType.TryMapTo(out CE.TextureSampleType sampleTypeNative).WithDebugAssertTrue();

            var bindingData = new CE.TextureBindingData
            {
                multisampled = multisampled,
                view_dimension = viewDimensionNative,
                sample_type = sampleTypeNative,
            };
            _entries[_index] = new CE.BindGroupLayoutEntry
            {
                binding = binding,
                visibility = visibilityNative,
                ty = CE.BindingType.Texture(&bindingData),
                count = 0,
            };
            _index++;
        }

        public void SetSamplerEntry(
            u32 binding,
            ShaderStages visibility,
            SamplerBindingType samplerBindingType)
        {
            visibility.TryMapTo(out Wgpu.ShaderStages visibilityNative).WithDebugAssertTrue();
            samplerBindingType.TryMapTo(out CE.SamplerBindingType samplerBindingTypeNative).WithDebugAssertTrue();
            _entries[_index] = new CE.BindGroupLayoutEntry
            {
                binding = binding,
                visibility = visibilityNative,
                ty = CE.BindingType.Sampler(&samplerBindingTypeNative),
                count = 0,
            };
            _index++;
        }

        public void SetBufferEntry(
            u32 binding,
            ShaderStages visibility,
            BufferBindingType bufferBindingType,
            bool hasDynamicOffset = false,
            u64 minBindingSize = 0)
        {
            visibility.TryMapTo(out Wgpu.ShaderStages visibilityNative).WithDebugAssertTrue();
            bufferBindingType.TryMapTo(out CE.BufferBindingType bufferBindingTypeNative).WithDebugAssertTrue();
            var bindingData = new CE.BufferBindingData
            {
                ty = bufferBindingTypeNative,
                has_dynamic_offset = hasDynamicOffset,
                min_binding_size = minBindingSize,
            };
            _entries[_index] = new CE.BindGroupLayoutEntry
            {
                binding = binding,
                visibility = visibilityNative,
                ty = CE.BindingType.Buffer(&bindingData),
                count = 0,
            };
            _index++;
        }
    }
}

[Flags]
public enum BufferUsages : u32
{
    [EnumMapTo(Wgpu.BufferUsages.MAP_READ)] MapRead = 1 << 0,
    [EnumMapTo(Wgpu.BufferUsages.MAP_WRITE)] MapWrite = 1 << 1,
    [EnumMapTo(Wgpu.BufferUsages.COPY_SRC)] CopySrc = 1 << 2,
    [EnumMapTo(Wgpu.BufferUsages.COPY_DST)] CopyDst = 1 << 3,
    [EnumMapTo(Wgpu.BufferUsages.INDEX)] Index = 1 << 4,
    [EnumMapTo(Wgpu.BufferUsages.VERTEX)] Vertex = 1 << 5,
    [EnumMapTo(Wgpu.BufferUsages.UNIFORM)] Uniform = 1 << 6,
    [EnumMapTo(Wgpu.BufferUsages.STORAGE)] Storage = 1 << 7,
    [EnumMapTo(Wgpu.BufferUsages.INDIRECT)] Indirect = 1 << 8,
}

[Flags]
public enum ShaderStages : u32
{
    [EnumMapTo(Wgpu.ShaderStages.NONE)] None = 0,
    [EnumMapTo(Wgpu.ShaderStages.VERTEX)] Vertex = 1 << 0,
    [EnumMapTo(Wgpu.ShaderStages.FRAGMENT)] Fragment = 1 << 1,
    [EnumMapTo(Wgpu.ShaderStages.COMPUTE)] Compute = 1 << 2,
}

public enum TextureSampleType
{
    [EnumMapTo(CE.TextureSampleType.FloatFilterable)] FloatFilterable = 0,
    [EnumMapTo(CE.TextureSampleType.FloatNotFilterable)] FloatNotFilterable = 1,
    [EnumMapTo(CE.TextureSampleType.Depth)] Depth = 2,
    [EnumMapTo(CE.TextureSampleType.Sint)] Sint = 3,
    [EnumMapTo(CE.TextureSampleType.Uint)] Uint = 4,
}

public enum TextureViewDimension
{
    [EnumMapTo(CE.TextureViewDimension.D1)] D1 = 0,
    [EnumMapTo(CE.TextureViewDimension.D2)] D2 = 1,
    [EnumMapTo(CE.TextureViewDimension.D2Array)] D2Array = 2,
    [EnumMapTo(CE.TextureViewDimension.Cube)] Cube = 3,
    [EnumMapTo(CE.TextureViewDimension.CubeArray)] CubeArray = 4,
    [EnumMapTo(CE.TextureViewDimension.D3)] D3 = 5,
}

public enum SamplerBindingType
{
    [EnumMapTo(CE.SamplerBindingType.Filtering)] Filtering = 0,
    [EnumMapTo(CE.SamplerBindingType.NonFiltering)] NonFiltering = 1,
    [EnumMapTo(CE.SamplerBindingType.Comparison)] Comparison = 2,
}

public enum BufferBindingType
{
    [EnumMapTo(CE.BufferBindingType.Uniform)] Uniform = 0,
    [EnumMapTo(CE.BufferBindingType.Storate)] Storate = 1,
    [EnumMapTo(CE.BufferBindingType.StorateReadOnly)] StorateReadOnly = 2,
}
