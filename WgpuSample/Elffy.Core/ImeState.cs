#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Buffers;
using System.Text.Unicode;

namespace Elffy;

internal unsafe sealed class ImeState
{
    private static readonly Encoding _utf8 = Encoding.UTF8;

    private byte* _buf;
    private usize _bufCapacity;
    private usize _len;

    private (usize Start, usize End)? _range;

    public ImeState()
    {
    }

    public void OnInput(in CE.ImeInputData input)
    {
        switch(input.tag) {
            case CE.ImeInputData.Tag.Enabled:
                break;
            case CE.ImeInputData.Tag.Preedit: {
                EnsureBufSize(input.text.len, false);
                Debug.Assert(_bufCapacity >= input.text.len);
                System.Buffer.MemoryCopy(input.text.data, _buf, _bufCapacity, input.text.len);
                _len = input.text.len;


                {
                    if(input.range.TryGetValue(out var range)) {
                        _range = (range.Start, range.End);
                    }
                    else {
                        _range = null;
                    }
                }


                if(_range.HasValue) {
                    var charCount = _utf8.GetCharCount(_buf, checked((int)_len));
                    var targetCharRange = (Start: 0, Length: 0);
                    // TODO: capture
                    var str = string.Create(charCount, (this, _range.Value), (dest, x) =>
                    {
                        var (self, range) = x;
                        var rangeStart = checked((int)range.Start);
                        var rangeEnd = checked((int)range.End);
                        var source = new ReadOnlySpan<byte>(self._buf, checked((int)self._len));

                        var charCount1 = Utf8ToUtf16(source[..rangeStart], dest[..], out _);
                        var charCount2 = Utf8ToUtf16(source[rangeStart..rangeEnd], dest[charCount1..], out _);
                        _ = Utf8ToUtf16(source[rangeEnd..], dest[(charCount1 + charCount2)..], out _);

                        targetCharRange.Start = charCount1;
                        targetCharRange.Length = charCount2;
                    });
                    Console.WriteLine($"Preedit: {str}");
                    Console.WriteLine("         " + new string('　', targetCharRange.Start) + new string('～', targetCharRange.Length));
                    Debug.WriteLine($"{targetCharRange.Start}, {targetCharRange.Length}");
                }
                else {
                    Console.WriteLine($"Preedit: ");
                }
                break;
            }
            case CE.ImeInputData.Tag.Commit: {
                Console.WriteLine($"Commit: {DumpToString()}");
                break;
            }
            case CE.ImeInputData.Tag.Disabled:
                break;
            default:
                break;
        }
    }

    private string DumpToString()
    {
        var len = _len;
        if(len == 0) {
            return "";
        }
        return _utf8.GetString(_buf, checked((int)len));
    }

    private int GetEncodedAsUtf16(Span<char> dest)
    {
        var len = _len;
        if(len == 0) {
            return 0;
        }
        var bufSpan = new Span<byte>(_buf, checked((int)len));
        switch(Utf8.ToUtf16(bufSpan, dest, out var _, out int writtenLen)) {
            case OperationStatus.Done:
                break;
            case OperationStatus.DestinationTooSmall:
                throw new ArgumentException("buffer is too short");
            case OperationStatus.NeedMoreData:
                break;
            case OperationStatus.InvalidData:
                throw new ArgumentException("failed to encode to utf16");
            default:
                break;
        }
        return writtenLen;
    }

    private static int Utf8ToUtf16(ReadOnlySpan<byte> source, Span<char> dest, out int bytesRead)
    {
        switch(Utf8.ToUtf16(source, dest, out bytesRead, out int writtenLen)) {
            case OperationStatus.Done:
                break;
            case OperationStatus.DestinationTooSmall:
                throw new ArgumentException("buffer is too short");
            case OperationStatus.NeedMoreData:
                throw new ArgumentException("insufficient byte sequence to encode to utf16");
            case OperationStatus.InvalidData:
                throw new ArgumentException("failed to encode to utf16");
            default:
                break;
        }
        return writtenLen;
    }

    private void EnsureBufSize(usize neededSize, bool copyData)
    {
        if(neededSize == 0) {
            return;
        }

        const usize MinCapacity = 128;

        var cap = _bufCapacity;
        if(cap < neededSize) {
            var newCap = usize.Max(neededSize, usize.Min(cap * 2, usize.MaxValue));
            newCap = usize.Max(newCap, neededSize);
            var newBuf = (byte*)AllocateMem(newCap);
            if(_len > 0 && copyData) {
                System.Buffer.MemoryCopy(_buf, newBuf, newCap, _len);
            }
            FreeMem(_buf);
            _buf = newBuf;
            _bufCapacity = newCap;
        }
    }

    private static void* AllocateMem(usize size) => NativeMemory.Alloc(size);
    private static void FreeMem(void* ptr) => NativeMemory.Free(ptr);
}
