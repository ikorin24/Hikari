#nullable enable

using System.Buffers;

namespace Hikari.Gltf.Internal;

internal unsafe interface ILargeBufferWriter<T> where T : unmanaged
{
    void Advance(nuint count);
    T* GetWrittenBufffer(out nuint count);
    T* GetBufferToWrite(nuint count, bool zeroCleared);
}
