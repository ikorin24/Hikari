#nullable enable
using Hikari.NativeBind;
using System;
using System.Text;

namespace Hikari;

public sealed class Keyboard
{
    private readonly Screen _screen;
    private readonly ImeState _imeState;
    private KeyActionFlag[] _prevAction;
    private bool[] _prevState;
    private KeyActionFlag[] _currentAction;
    private bool[] _currentState;

    internal Keyboard(Screen screen)
    {
        _screen = screen;
        _imeState = new ImeState(screen);

        const int N = SequentialSetCount.KeyCode;
        _prevAction = new KeyActionFlag[N];
        _prevState = new bool[N];
        _currentAction = new KeyActionFlag[N];
        _currentState = new bool[N];
    }

    internal void OnImeInput(in CH.ImeInputData input)
    {
        _imeState.OnInput(input);
    }

    internal void OnCharReceived(Rune input)
    {
        if(Rune.IsValid(input.Value) == false) { return; }
        if(Rune.IsControl(input)) {
            // TODO:
        }
        else {
            // TODO:
        }
    }

    internal void OnKeyboardInput(CH.KeyCode key, bool pressed)
    {
        KeyCode keycode = key.MapOrThrow();
        var keyIndex = (u32)keycode;
        if(pressed) {
            _currentAction[keyIndex] |= KeyActionFlag.Pressed;
        }
        else {
            _currentAction[keyIndex] |= KeyActionFlag.Released;
        }
        _currentState[keyIndex] = pressed;
    }

    internal void PrepareNextFrame()
    {
        (_prevAction, _currentAction) = (_currentAction, _prevAction);
        _currentAction.AsSpan().Clear();
        (_prevState, _currentState) = (_currentState, _prevState);
        _prevState.AsSpan().CopyTo(_currentState);
    }

    public bool IsDown(KeyCode key)
    {
        var keyIndex = (u32)key;
        return
            _prevState[keyIndex] == false &&
            _currentAction[keyIndex].HasFlag(KeyActionFlag.Pressed);
    }

    public bool IsUp(KeyCode key)
    {
        var keyIndex = (u32)key;
        return
            _prevState[keyIndex] == true &&
            _currentAction[keyIndex].HasFlag(KeyActionFlag.Released);
    }

    public bool IsPressed(KeyCode key)
    {
        return _currentState[(u32)key];
    }


    [Flags]
    private enum KeyActionFlag : byte
    {
        None = 0,
        Pressed = 1 << 0,
        Released = 1 << 1,
    }
}

[SequentialSet]
public enum KeyCode
{
    [EnumMapTo(CH.KeyCode.Backquote)] Backquote = 0,
    [EnumMapTo(CH.KeyCode.Backslash)] Backslash = 1,
    [EnumMapTo(CH.KeyCode.BracketLeft)] BracketLeft = 2,
    [EnumMapTo(CH.KeyCode.BracketRight)] BracketRight = 3,
    [EnumMapTo(CH.KeyCode.Comma)] Comma = 4,
    [EnumMapTo(CH.KeyCode.Digit0)] Digit0 = 5,
    [EnumMapTo(CH.KeyCode.Digit1)] Digit1 = 6,
    [EnumMapTo(CH.KeyCode.Digit2)] Digit2 = 7,
    [EnumMapTo(CH.KeyCode.Digit3)] Digit3 = 8,
    [EnumMapTo(CH.KeyCode.Digit4)] Digit4 = 9,
    [EnumMapTo(CH.KeyCode.Digit5)] Digit5 = 10,
    [EnumMapTo(CH.KeyCode.Digit6)] Digit6 = 11,
    [EnumMapTo(CH.KeyCode.Digit7)] Digit7 = 12,
    [EnumMapTo(CH.KeyCode.Digit8)] Digit8 = 13,
    [EnumMapTo(CH.KeyCode.Digit9)] Digit9 = 14,
    [EnumMapTo(CH.KeyCode.Equal)] Equal = 15,
    [EnumMapTo(CH.KeyCode.IntlBackslash)] IntlBackslash = 16,
    [EnumMapTo(CH.KeyCode.IntlRo)] IntlRo = 17,
    [EnumMapTo(CH.KeyCode.IntlYen)] IntlYen = 18,
    [EnumMapTo(CH.KeyCode.KeyA)] KeyA = 19,
    [EnumMapTo(CH.KeyCode.KeyB)] KeyB = 20,
    [EnumMapTo(CH.KeyCode.KeyC)] KeyC = 21,
    [EnumMapTo(CH.KeyCode.KeyD)] KeyD = 22,
    [EnumMapTo(CH.KeyCode.KeyE)] KeyE = 23,
    [EnumMapTo(CH.KeyCode.KeyF)] KeyF = 24,
    [EnumMapTo(CH.KeyCode.KeyG)] KeyG = 25,
    [EnumMapTo(CH.KeyCode.KeyH)] KeyH = 26,
    [EnumMapTo(CH.KeyCode.KeyI)] KeyI = 27,
    [EnumMapTo(CH.KeyCode.KeyJ)] KeyJ = 28,
    [EnumMapTo(CH.KeyCode.KeyK)] KeyK = 29,
    [EnumMapTo(CH.KeyCode.KeyL)] KeyL = 30,
    [EnumMapTo(CH.KeyCode.KeyM)] KeyM = 31,
    [EnumMapTo(CH.KeyCode.KeyN)] KeyN = 32,
    [EnumMapTo(CH.KeyCode.KeyO)] KeyO = 33,
    [EnumMapTo(CH.KeyCode.KeyP)] KeyP = 34,
    [EnumMapTo(CH.KeyCode.KeyQ)] KeyQ = 35,
    [EnumMapTo(CH.KeyCode.KeyR)] KeyR = 36,
    [EnumMapTo(CH.KeyCode.KeyS)] KeyS = 37,
    [EnumMapTo(CH.KeyCode.KeyT)] KeyT = 38,
    [EnumMapTo(CH.KeyCode.KeyU)] KeyU = 39,
    [EnumMapTo(CH.KeyCode.KeyV)] KeyV = 40,
    [EnumMapTo(CH.KeyCode.KeyW)] KeyW = 41,
    [EnumMapTo(CH.KeyCode.KeyX)] KeyX = 42,
    [EnumMapTo(CH.KeyCode.KeyY)] KeyY = 43,
    [EnumMapTo(CH.KeyCode.KeyZ)] KeyZ = 44,
    [EnumMapTo(CH.KeyCode.Minus)] Minus = 45,
    [EnumMapTo(CH.KeyCode.Period)] Period = 46,
    [EnumMapTo(CH.KeyCode.Quote)] Quote = 47,
    [EnumMapTo(CH.KeyCode.Semicolon)] Semicolon = 48,
    [EnumMapTo(CH.KeyCode.Slash)] Slash = 49,
    [EnumMapTo(CH.KeyCode.AltLeft)] AltLeft = 50,
    [EnumMapTo(CH.KeyCode.AltRight)] AltRight = 51,
    [EnumMapTo(CH.KeyCode.Backspace)] Backspace = 52,
    [EnumMapTo(CH.KeyCode.CapsLock)] CapsLock = 53,
    [EnumMapTo(CH.KeyCode.ContextMenu)] ContextMenu = 54,
    [EnumMapTo(CH.KeyCode.ControlLeft)] ControlLeft = 55,
    [EnumMapTo(CH.KeyCode.ControlRight)] ControlRight = 56,
    [EnumMapTo(CH.KeyCode.Enter)] Enter = 57,
    [EnumMapTo(CH.KeyCode.SuperLeft)] SuperLeft = 58,
    [EnumMapTo(CH.KeyCode.SuperRight)] SuperRight = 59,
    [EnumMapTo(CH.KeyCode.ShiftLeft)] ShiftLeft = 60,
    [EnumMapTo(CH.KeyCode.ShiftRight)] ShiftRight = 61,
    [EnumMapTo(CH.KeyCode.Space)] Space = 62,
    [EnumMapTo(CH.KeyCode.Tab)] Tab = 63,
    [EnumMapTo(CH.KeyCode.Convert)] Convert = 64,
    [EnumMapTo(CH.KeyCode.KanaMode)] KanaMode = 65,
    [EnumMapTo(CH.KeyCode.Lang1)] Lang1 = 66,
    [EnumMapTo(CH.KeyCode.Lang2)] Lang2 = 67,
    [EnumMapTo(CH.KeyCode.Lang3)] Lang3 = 68,
    [EnumMapTo(CH.KeyCode.Lang4)] Lang4 = 69,
    [EnumMapTo(CH.KeyCode.Lang5)] Lang5 = 70,
    [EnumMapTo(CH.KeyCode.NonConvert)] NonConvert = 71,
    [EnumMapTo(CH.KeyCode.Delete)] Delete = 72,
    [EnumMapTo(CH.KeyCode.End)] End = 73,
    [EnumMapTo(CH.KeyCode.Help)] Help = 74,
    [EnumMapTo(CH.KeyCode.Home)] Home = 75,
    [EnumMapTo(CH.KeyCode.Insert)] Insert = 76,
    [EnumMapTo(CH.KeyCode.PageDown)] PageDown = 77,
    [EnumMapTo(CH.KeyCode.PageUp)] PageUp = 78,
    [EnumMapTo(CH.KeyCode.ArrowDown)] ArrowDown = 79,
    [EnumMapTo(CH.KeyCode.ArrowLeft)] ArrowLeft = 80,
    [EnumMapTo(CH.KeyCode.ArrowRight)] ArrowRight = 81,
    [EnumMapTo(CH.KeyCode.ArrowUp)] ArrowUp = 82,
    [EnumMapTo(CH.KeyCode.NumLock)] NumLock = 83,
    [EnumMapTo(CH.KeyCode.Numpad0)] Numpad0 = 84,
    [EnumMapTo(CH.KeyCode.Numpad1)] Numpad1 = 85,
    [EnumMapTo(CH.KeyCode.Numpad2)] Numpad2 = 86,
    [EnumMapTo(CH.KeyCode.Numpad3)] Numpad3 = 87,
    [EnumMapTo(CH.KeyCode.Numpad4)] Numpad4 = 88,
    [EnumMapTo(CH.KeyCode.Numpad5)] Numpad5 = 89,
    [EnumMapTo(CH.KeyCode.Numpad6)] Numpad6 = 90,
    [EnumMapTo(CH.KeyCode.Numpad7)] Numpad7 = 91,
    [EnumMapTo(CH.KeyCode.Numpad8)] Numpad8 = 92,
    [EnumMapTo(CH.KeyCode.Numpad9)] Numpad9 = 93,
    [EnumMapTo(CH.KeyCode.NumpadAdd)] NumpadAdd = 94,
    [EnumMapTo(CH.KeyCode.NumpadBackspace)] NumpadBackspace = 95,
    [EnumMapTo(CH.KeyCode.NumpadClear)] NumpadClear = 96,
    [EnumMapTo(CH.KeyCode.NumpadClearEntry)] NumpadClearEntry = 97,
    [EnumMapTo(CH.KeyCode.NumpadComma)] NumpadComma = 98,
    [EnumMapTo(CH.KeyCode.NumpadDecimal)] NumpadDecimal = 99,
    [EnumMapTo(CH.KeyCode.NumpadDivide)] NumpadDivide = 100,
    [EnumMapTo(CH.KeyCode.NumpadEnter)] NumpadEnter = 101,
    [EnumMapTo(CH.KeyCode.NumpadEqual)] NumpadEqual = 102,
    [EnumMapTo(CH.KeyCode.NumpadHash)] NumpadHash = 103,
    [EnumMapTo(CH.KeyCode.NumpadMemoryAdd)] NumpadMemoryAdd = 104,
    [EnumMapTo(CH.KeyCode.NumpadMemoryClear)] NumpadMemoryClear = 105,
    [EnumMapTo(CH.KeyCode.NumpadMemoryRecall)] NumpadMemoryRecall = 106,
    [EnumMapTo(CH.KeyCode.NumpadMemoryStore)] NumpadMemoryStore = 107,
    [EnumMapTo(CH.KeyCode.NumpadMemorySubtract)] NumpadMemorySubtract = 108,
    [EnumMapTo(CH.KeyCode.NumpadMultiply)] NumpadMultiply = 109,
    [EnumMapTo(CH.KeyCode.NumpadParenLeft)] NumpadParenLeft = 110,
    [EnumMapTo(CH.KeyCode.NumpadParenRight)] NumpadParenRight = 111,
    [EnumMapTo(CH.KeyCode.NumpadStar)] NumpadStar = 112,
    [EnumMapTo(CH.KeyCode.NumpadSubtract)] NumpadSubtract = 113,
    [EnumMapTo(CH.KeyCode.Escape)] Escape = 114,
    [EnumMapTo(CH.KeyCode.Fn)] Fn = 115,
    [EnumMapTo(CH.KeyCode.FnLock)] FnLock = 116,
    [EnumMapTo(CH.KeyCode.PrintScreen)] PrintScreen = 117,
    [EnumMapTo(CH.KeyCode.ScrollLock)] ScrollLock = 118,
    [EnumMapTo(CH.KeyCode.Pause)] Pause = 119,
    [EnumMapTo(CH.KeyCode.BrowserBack)] BrowserBack = 120,
    [EnumMapTo(CH.KeyCode.BrowserFavorites)] BrowserFavorites = 121,
    [EnumMapTo(CH.KeyCode.BrowserForward)] BrowserForward = 122,
    [EnumMapTo(CH.KeyCode.BrowserHome)] BrowserHome = 123,
    [EnumMapTo(CH.KeyCode.BrowserRefresh)] BrowserRefresh = 124,
    [EnumMapTo(CH.KeyCode.BrowserSearch)] BrowserSearch = 125,
    [EnumMapTo(CH.KeyCode.BrowserStop)] BrowserStop = 126,
    [EnumMapTo(CH.KeyCode.Eject)] Eject = 127,
    [EnumMapTo(CH.KeyCode.LaunchApp1)] LaunchApp1 = 128,
    [EnumMapTo(CH.KeyCode.LaunchApp2)] LaunchApp2 = 129,
    [EnumMapTo(CH.KeyCode.LaunchMail)] LaunchMail = 130,
    [EnumMapTo(CH.KeyCode.MediaPlayPause)] MediaPlayPause = 131,
    [EnumMapTo(CH.KeyCode.MediaSelect)] MediaSelect = 132,
    [EnumMapTo(CH.KeyCode.MediaStop)] MediaStop = 133,
    [EnumMapTo(CH.KeyCode.MediaTrackNext)] MediaTrackNext = 134,
    [EnumMapTo(CH.KeyCode.MediaTrackPrevious)] MediaTrackPrevious = 135,
    [EnumMapTo(CH.KeyCode.Power)] Power = 136,
    [EnumMapTo(CH.KeyCode.Sleep)] Sleep = 137,
    [EnumMapTo(CH.KeyCode.AudioVolumeDown)] AudioVolumeDown = 138,
    [EnumMapTo(CH.KeyCode.AudioVolumeMute)] AudioVolumeMute = 139,
    [EnumMapTo(CH.KeyCode.AudioVolumeUp)] AudioVolumeUp = 140,
    [EnumMapTo(CH.KeyCode.WakeUp)] WakeUp = 141,
    [EnumMapTo(CH.KeyCode.Meta)] Meta = 142,
    [EnumMapTo(CH.KeyCode.Hyper)] Hyper = 143,
    [EnumMapTo(CH.KeyCode.Turbo)] Turbo = 144,
    [EnumMapTo(CH.KeyCode.Abort)] Abort = 145,
    [EnumMapTo(CH.KeyCode.Resume)] Resume = 146,
    [EnumMapTo(CH.KeyCode.Suspend)] Suspend = 147,
    [EnumMapTo(CH.KeyCode.Again)] Again = 148,
    [EnumMapTo(CH.KeyCode.Copy)] Copy = 149,
    [EnumMapTo(CH.KeyCode.Cut)] Cut = 150,
    [EnumMapTo(CH.KeyCode.Find)] Find = 151,
    [EnumMapTo(CH.KeyCode.Open)] Open = 152,
    [EnumMapTo(CH.KeyCode.Paste)] Paste = 153,
    [EnumMapTo(CH.KeyCode.Props)] Props = 154,
    [EnumMapTo(CH.KeyCode.Select)] Select = 155,
    [EnumMapTo(CH.KeyCode.Undo)] Undo = 156,
    [EnumMapTo(CH.KeyCode.Hiragana)] Hiragana = 157,
    [EnumMapTo(CH.KeyCode.Katakana)] Katakana = 158,
    [EnumMapTo(CH.KeyCode.F1)] F1 = 159,
    [EnumMapTo(CH.KeyCode.F2)] F2 = 160,
    [EnumMapTo(CH.KeyCode.F3)] F3 = 161,
    [EnumMapTo(CH.KeyCode.F4)] F4 = 162,
    [EnumMapTo(CH.KeyCode.F5)] F5 = 163,
    [EnumMapTo(CH.KeyCode.F6)] F6 = 164,
    [EnumMapTo(CH.KeyCode.F7)] F7 = 165,
    [EnumMapTo(CH.KeyCode.F8)] F8 = 166,
    [EnumMapTo(CH.KeyCode.F9)] F9 = 167,
    [EnumMapTo(CH.KeyCode.F10)] F10 = 168,
    [EnumMapTo(CH.KeyCode.F11)] F11 = 169,
    [EnumMapTo(CH.KeyCode.F12)] F12 = 170,
    [EnumMapTo(CH.KeyCode.F13)] F13 = 171,
    [EnumMapTo(CH.KeyCode.F14)] F14 = 172,
    [EnumMapTo(CH.KeyCode.F15)] F15 = 173,
    [EnumMapTo(CH.KeyCode.F16)] F16 = 174,
    [EnumMapTo(CH.KeyCode.F17)] F17 = 175,
    [EnumMapTo(CH.KeyCode.F18)] F18 = 176,
    [EnumMapTo(CH.KeyCode.F19)] F19 = 177,
    [EnumMapTo(CH.KeyCode.F20)] F20 = 178,
    [EnumMapTo(CH.KeyCode.F21)] F21 = 179,
    [EnumMapTo(CH.KeyCode.F22)] F22 = 180,
    [EnumMapTo(CH.KeyCode.F23)] F23 = 181,
    [EnumMapTo(CH.KeyCode.F24)] F24 = 182,
    [EnumMapTo(CH.KeyCode.F25)] F25 = 183,
    [EnumMapTo(CH.KeyCode.F26)] F26 = 184,
    [EnumMapTo(CH.KeyCode.F27)] F27 = 185,
    [EnumMapTo(CH.KeyCode.F28)] F28 = 186,
    [EnumMapTo(CH.KeyCode.F29)] F29 = 187,
    [EnumMapTo(CH.KeyCode.F30)] F30 = 188,
    [EnumMapTo(CH.KeyCode.F31)] F31 = 189,
    [EnumMapTo(CH.KeyCode.F32)] F32 = 190,
    [EnumMapTo(CH.KeyCode.F33)] F33 = 191,
    [EnumMapTo(CH.KeyCode.F34)] F34 = 192,
    [EnumMapTo(CH.KeyCode.F35)] F35 = 193,
}
