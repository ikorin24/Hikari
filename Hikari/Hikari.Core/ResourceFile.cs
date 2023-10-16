#nullable enable
using Microsoft.Win32.SafeHandles;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;

namespace Hikari;

[DebuggerDisplay("{DebugView,nq}")]
public readonly record struct ResourceFile
{
    private const char PathSeparater = '/';
    private const char Dot = '.';

    private readonly IResourcePackage? _package;
    private readonly string? _name;

    public static ResourceFile None => default;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebugView => _package is null ? "<empty>" : $"\"{Name}\" (in {_package.Name})";

    public string Name => _name ?? "";

    public bool IsNone => _package is null;

    public long FileSize => Package.TryGetSize(_name, out var size) ? size : 0;

    public IResourcePackage Package => _package ?? EmptyResourcePackage.Instance;

    internal ResourceFile(IResourcePackage package, string name)
    {
        _package = package;
        _name = name;
    }

    public ReadOnlySpan<char> GetFileExtension()
    {
        var index = _name?.LastIndexOf(Dot) ?? -1;
        return index >= 0 ? _name.AsSpan(index) : ReadOnlySpan<char>.Empty;
    }

    public static void ThrowArgumentExceptionIfNone(ResourceFile resource, [CallerArgumentExpression(nameof(resource))] string? paramName = null)
    {
        if(resource.IsNone) {
            Throw(paramName);
        }
        [DoesNotReturn] static void Throw(string? paramName) => throw new ArgumentException(paramName);
    }

    public int ReadBytes(Span<byte> buffer) => ReadBytes(buffer, 0);

    public int ReadBytes(Span<byte> buffer, long fileOffset)
    {
        unsafe {
            fixed(byte* ptr = buffer) {
                return (int)ReadBytes((IntPtr)ptr, (nuint)buffer.Length, fileOffset);
            }
        }
    }

    public long ReadBytes(IntPtr buffer, nuint bufferLength) => ReadBytes(buffer, bufferLength, 0);

    public long ReadBytes(IntPtr buffer, nuint bufferLength, long fileOffset)
    {
        if(TryGetHandle(out var handle)) {
            try {
                return handle.Read(buffer, bufferLength, fileOffset);
            }
            finally {
                handle.Dispose();
            }
        }
        else {
            using var stream = GetStream();
            return ReadStreamAll(stream, fileOffset, buffer, (long)bufferLength);
        }

        static unsafe long ReadStreamAll(Stream stream, long fileOffset, IntPtr buffer, long bufferLength)
        {
            if(fileOffset != 0) {
                if(stream.CanSeek) {
                    stream.Position = fileOffset;
                }
                else {
                    throw new NotSupportedException("Cannot seek the specified stream.");    // TODO: unseekable stream
                }
            }

            long totalLen = 0;
            while(true) {
                var dest = (byte*)buffer + totalLen;
                var destLen = (int)Math.Min(bufferLength - totalLen, int.MaxValue);
                var readLen = stream.Read(new Span<byte>(dest, destLen));
                totalLen += readLen;
                if(totalLen == bufferLength || readLen == 0) {
                    return totalLen;
                }
            }
        }
    }

    public bool TryGetHandle(out ResourceFileHandle handle) => Package.TryGetHandle(_name, out handle);

    public ResourceFileHandle GetHandle() => Package.TryGetHandle(_name, out var handle) ? handle : ResourceFileHandle.None;

    public Stream GetStream() => Package.TryGetStream(_name, out var stream) ? stream : Stream.Null;

    private sealed class EmptyResourcePackage : IResourcePackage
    {
        private static readonly EmptyResourcePackage _instance = new EmptyResourcePackage();
        public static EmptyResourcePackage Instance => _instance;

        public string Name => "Empty";

        public bool Exists(string? name) => false;

        public bool TryGetHandle(string? name, out ResourceFileHandle handle)
        {
            handle = ResourceFileHandle.None;
            return false;
        }

        public bool TryGetSize(string? name, out long size)
        {
            size = 0;
            return false;
        }

        public bool TryGetStream(string? name, out Stream stream)
        {
            stream = Stream.Null;
            return false;
        }
    }
}


public interface IResourcePackage
{
    string Name { get; }
    bool TryGetHandle(string? name, out ResourceFileHandle handle);
    bool TryGetStream(string? name, out Stream stream);
    bool TryGetSize(string? name, out long size);
    bool Exists(string? name);

    public ResourceFile this[string name] => GetFile(name);

    public bool TryGetFile(string name, out ResourceFile file)
    {
        if(Exists(name) == false) {
            file = ResourceFile.None;
            return false;
        }
        file = new ResourceFile(this, name);
        return true;
    }

    public ResourceFile GetFile(string name)
    {
        if(TryGetFile(name, out var file) == false) {
            ThrowNotFound(name);

            [DoesNotReturn] static void ThrowNotFound(string name) => throw new ArgumentException($"Resource file \"{name}\" is not found.");
        }
        return file;
    }
}

public readonly struct ResourceFileHandle : IDisposable, IEquatable<ResourceFileHandle>
{
    private readonly SafeFileHandle? _handle;
    private readonly long _offset;
    private readonly long _size;

    public static ResourceFileHandle None => default;

    public long FileSize => _size;

    [Obsolete("Don't use default constructor", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ResourceFileHandle() => throw new NotSupportedException("Don't use default constructor");

    internal ResourceFileHandle(SafeFileHandle handle, long offset, long resourceFileSize)
    {
        _handle = handle;
        _offset = offset;
        _size = resourceFileSize;
    }

    public int Read(Span<byte> buffer, long fileOffset)
    {
        if((ulong)fileOffset >= (ulong)_size) {
            ThrowArgOutOfRange();
            static void ThrowArgOutOfRange() => throw new ArgumentOutOfRangeException(nameof(fileOffset));
        }
        var readBuf = buffer.Slice(0, (int)Math.Min(buffer.Length, _size));
        var handle = ValidateHandle();
        var actualOffset = _offset + fileOffset;
        return RandomAccess.Read(handle, readBuf, actualOffset);
    }

    public unsafe long Read(IntPtr buffer, nuint bufferLength, long fileOffset) => Read(buffer.ToPointer(), bufferLength, fileOffset);

    public unsafe long Read(void* buffer, nuint bufferLength, long fileOffset)
    {
        if(bufferLength <= int.MaxValue) {
            return Read(new Span<byte>(buffer, (int)bufferLength), fileOffset);
        }
        else {
            if((ulong)fileOffset >= (ulong)_size) {
                ThrowArgOutOfRange();
                static void ThrowArgOutOfRange() => throw new ArgumentOutOfRangeException(nameof(fileOffset));
            }
            var readBufLen = Math.Min((ulong)bufferLength, (ulong)_size);

            var handle = ValidateHandle();
            var memCount = readBufLen >> 31;
            Debug.Assert(memCount > 0);
            var memoryArray = new Memory<byte>[memCount];
            for(int i = 0; i < memoryArray.Length; i++) {
                ulong offset = ((ulong)i << 31);
                byte* ptr = (byte*)buffer + offset;
                int len = (int)(readBufLen - offset);
                var memoryManager = new PointerMemoryManager(ptr, len);
                memoryArray[i] = memoryManager.Memory;
            }
            return RandomAccess.Read(handle, memoryArray, fileOffset);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SafeFileHandle ValidateHandle() => _handle ?? throw new InvalidOperationException("File handle is null");

    public void Dispose()
    {
        _handle?.Dispose();
    }

    public override bool Equals(object? obj) => obj is ResourceFileHandle handle && Equals(handle);

    public bool Equals(ResourceFileHandle other) => (_handle == other._handle) && (_offset == other._offset) && (_size == other._size);

    public override int GetHashCode() => HashCode.Combine(_handle, _offset, _size);

    public static bool operator ==(ResourceFileHandle left, ResourceFileHandle right) => left.Equals(right);

    public static bool operator !=(ResourceFileHandle left, ResourceFileHandle right) => !(left == right);

    private unsafe sealed class PointerMemoryManager : MemoryManager<byte>
    {
        private byte* _ptr;
        private int _len;

        public PointerMemoryManager(byte* ptr, int len)
        {
            _ptr = ptr;
            _len = len;
        }

        public override Span<byte> GetSpan() => new Span<byte>(_ptr, _len);

        public override MemoryHandle Pin(int elementIndex = 0) => new MemoryHandle(_ptr);

        public override void Unpin()
        {
            // nop
        }

        protected override void Dispose(bool disposing)
        {
            _ptr = null;
            _len = 0;
        }
    }
}
