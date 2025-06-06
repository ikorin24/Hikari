﻿#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Hikari.NativeBind;

namespace Hikari;

internal unsafe sealed class ImeState : IImePreeditState
{
    private static readonly Encoding _utf8 = Encoding.UTF8;

    private readonly Screen _screen;
    private byte* _buf;
    private int _bufCapacity;
    private int _len;
    private Range? _cursorBufRange;
    private Range? _cursorStringRange;

    private Span<byte> BufferSpan => new Span<byte>(_buf, _bufCapacity);
    private ReadOnlySpan<byte> TextUtf8Span => new ReadOnlySpan<byte>(_buf, _len);
    private ReadOnlySpan<byte> CursorTextUtf8Span => _cursorBufRange.HasValue ? TextUtf8Span[_cursorBufRange.Value] : ReadOnlySpan<byte>.Empty;

    public event Action<Screen>? Start;
    public event Action<Screen>? End;
    public event Action<IImePreeditState, Screen>? Preedit;
    public event ReadOnlySpanAction<byte, Screen>? Commit;

    internal ImeState(Screen screen)
    {
        _screen = screen;
    }

    ~ImeState()
    {
        // TODO:
        FreeMem(_buf);
    }

    string IImePreeditState.GetText()
    {
        var text = _utf8.GetString(TextUtf8Span);
        return text;
    }

    bool IImePreeditState.TryGetCursor(out Range cursorRange)
    {
        var r = _cursorStringRange;
        if(r.HasValue) {
            cursorRange = r.Value;
            return true;
        }
        else {
            cursorRange = default;
            return false;
        }
    }

    public void OnInput(in CH.ImeInputData input)
    {
        Range? range = input.range.TryGetValue(out var r) ? new Range(r.Start.ToInt32(), r.End.ToInt32()) : null;

        var textUtf8 = new ReadOnlySpan<byte>(input.text.data, input.text.len.ToInt32());

        if(input.tag != CH.ImeInputData.Tag.Disabled) {
            EngineCore.SetImePosition(_screen.AsRefChecked(), 10, 10);
        }

        switch(input.tag) {
            case CH.ImeInputData.Tag.Enabled: {
                Start?.Invoke(_screen);
                break;
            }
            case CH.ImeInputData.Tag.Preedit: {
                EnsureBufSize(textUtf8.Length, false);
                Debug.Assert(_bufCapacity >= textUtf8.Length);
                textUtf8.CopyTo(BufferSpan);
                _len = textUtf8.Length;
                _cursorBufRange = range;
                if(range.HasValue) {
                    int rangeStart = _utf8.GetCharCount(textUtf8[0..range.Value.Start]);
                    int rangeLength = _utf8.GetCharCount(textUtf8[range.Value]);
                    _cursorStringRange = new Range(rangeStart, rangeStart + rangeLength);
                }
                else {
                    _cursorStringRange = null;
                }
                Preedit?.Invoke(this, _screen);
                break;
            }
            case CH.ImeInputData.Tag.Commit: {
                Commit?.Invoke(textUtf8, _screen);
                break;
            }
            case CH.ImeInputData.Tag.Disabled:
                End?.Invoke(_screen);
                break;
            default:
                break;
        }
    }

    private void EnsureBufSize(int neededSize, bool copyData)
    {
        Debug.Assert(neededSize >= 0);
        if(neededSize == 0) {
            return;
        }

        const int MinCapacity = 128;

        var cap = _bufCapacity;
        if(cap < neededSize) {
            int newCap = int.Max(neededSize, (int)uint.Min((uint)cap * 2, int.MaxValue));
            newCap = int.Max(newCap, MinCapacity);
            var newBuf = (byte*)AllocateMem((usize)newCap);
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

internal interface IImePreeditState
{
    string GetText();
    bool TryGetCursor(out Range cursorRange);
}
