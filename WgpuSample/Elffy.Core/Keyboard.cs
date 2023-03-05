#nullable enable
using Elffy.NativeBind;
using System;
using System.Text;

namespace Elffy;

public sealed class Keyboard
{
    private readonly IHostScreen _screen;
    private readonly ImeState _imeState;
    private KeyActionFlag[] _prevAction;
    private bool[] _prevState;
    private KeyActionFlag[] _currentAction;
    private bool[] _currentState;

    internal Keyboard(IHostScreen screen)
    {
        _screen = screen;
        _imeState = new ImeState(screen);

        int n = Enum.GetValues<Winit.VirtualKeyCode>().Length;  // TODO: define it at compile time
        _prevAction = new KeyActionFlag[n];
        _prevState = new bool[n];
        _currentAction = new KeyActionFlag[n];
        _currentState = new bool[n];
    }

    internal void OnImeInput(in CE.ImeInputData input)
    {
        _imeState.OnInput(input);
    }

    internal void OnCharReceived(Rune input)
    {
    }

    internal void OnKeyboardInput(Winit.VirtualKeyCode key, bool pressed)
    {
        if(pressed) {
            _currentAction[(u32)key] |= KeyActionFlag.Pressed;
        }
        else {
            _currentAction[(u32)key] |= KeyActionFlag.Released;
        }
        _currentState[(u32)key] = pressed;
    }

    internal void InitFrame()
    {
        (_prevAction, _currentAction) = (_currentAction, _prevAction);
        _currentAction.AsSpan().Clear();
        (_prevState, _currentState) = (_currentState, _prevState);
        _prevState.AsSpan().CopyTo(_currentState);
    }

    public bool IsDown(Keys key) => IsDown(key.MapOrThrow());

    internal bool IsDown(Winit.VirtualKeyCode key)
    {
        var index = (u32)key;
        return
            _prevState[index] == false &&
            _currentAction[index].HasFlag(KeyActionFlag.Pressed);
    }

    public bool IsUp(Keys key) => IsUp(key.MapOrThrow());

    internal bool IsUp(Winit.VirtualKeyCode key)
    {
        var index = (u32)key;
        return
            _prevState[index] == true &&
            _currentAction[index].HasFlag(KeyActionFlag.Released);
    }

    public bool IsPressed(Keys key) => IsPressed(key.MapOrThrow());

    internal bool IsPressed(Winit.VirtualKeyCode key)
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

// const int SequentialSet_Keys.Count;
// SequentialSet<Keys>.Count();
// SequentialSet<Keys>.TryGetCount(out _);
//
//[SequentialSet]
public enum Keys
{
    [EnumMapTo(Winit.VirtualKeyCode.Key1)] Key1,
    [EnumMapTo(Winit.VirtualKeyCode.Key2)] Key2,
    [EnumMapTo(Winit.VirtualKeyCode.Key3)] Key3,
    [EnumMapTo(Winit.VirtualKeyCode.Key4)] Key4,
    [EnumMapTo(Winit.VirtualKeyCode.Key5)] Key5,
    [EnumMapTo(Winit.VirtualKeyCode.Key6)] Key6,
    [EnumMapTo(Winit.VirtualKeyCode.Key7)] Key7,
    [EnumMapTo(Winit.VirtualKeyCode.Key8)] Key8,
    [EnumMapTo(Winit.VirtualKeyCode.Key9)] Key9,
    [EnumMapTo(Winit.VirtualKeyCode.Key0)] Key0,
    [EnumMapTo(Winit.VirtualKeyCode.A)] A,
    [EnumMapTo(Winit.VirtualKeyCode.B)] B,
    [EnumMapTo(Winit.VirtualKeyCode.C)] C,
    [EnumMapTo(Winit.VirtualKeyCode.D)] D,
    [EnumMapTo(Winit.VirtualKeyCode.E)] E,
    [EnumMapTo(Winit.VirtualKeyCode.F)] F,
    [EnumMapTo(Winit.VirtualKeyCode.G)] G,
    [EnumMapTo(Winit.VirtualKeyCode.H)] H,
    [EnumMapTo(Winit.VirtualKeyCode.I)] I,
    [EnumMapTo(Winit.VirtualKeyCode.J)] J,
    [EnumMapTo(Winit.VirtualKeyCode.K)] K,
    [EnumMapTo(Winit.VirtualKeyCode.L)] L,
    [EnumMapTo(Winit.VirtualKeyCode.M)] M,
    [EnumMapTo(Winit.VirtualKeyCode.N)] N,
    [EnumMapTo(Winit.VirtualKeyCode.O)] O,
    [EnumMapTo(Winit.VirtualKeyCode.P)] P,
    [EnumMapTo(Winit.VirtualKeyCode.Q)] Q,
    [EnumMapTo(Winit.VirtualKeyCode.R)] R,
    [EnumMapTo(Winit.VirtualKeyCode.S)] S,
    [EnumMapTo(Winit.VirtualKeyCode.T)] T,
    [EnumMapTo(Winit.VirtualKeyCode.U)] U,
    [EnumMapTo(Winit.VirtualKeyCode.V)] V,
    [EnumMapTo(Winit.VirtualKeyCode.W)] W,
    [EnumMapTo(Winit.VirtualKeyCode.X)] X,
    [EnumMapTo(Winit.VirtualKeyCode.Y)] Y,
    [EnumMapTo(Winit.VirtualKeyCode.Z)] Z,
    [EnumMapTo(Winit.VirtualKeyCode.Escape)] Escape,
    [EnumMapTo(Winit.VirtualKeyCode.F1)] F1,
    [EnumMapTo(Winit.VirtualKeyCode.F2)] F2,
    [EnumMapTo(Winit.VirtualKeyCode.F3)] F3,
    [EnumMapTo(Winit.VirtualKeyCode.F4)] F4,
    [EnumMapTo(Winit.VirtualKeyCode.F5)] F5,
    [EnumMapTo(Winit.VirtualKeyCode.F6)] F6,
    [EnumMapTo(Winit.VirtualKeyCode.F7)] F7,
    [EnumMapTo(Winit.VirtualKeyCode.F8)] F8,
    [EnumMapTo(Winit.VirtualKeyCode.F9)] F9,
    [EnumMapTo(Winit.VirtualKeyCode.F10)] F10,
    [EnumMapTo(Winit.VirtualKeyCode.F11)] F11,
    [EnumMapTo(Winit.VirtualKeyCode.F12)] F12,
    [EnumMapTo(Winit.VirtualKeyCode.F13)] F13,
    [EnumMapTo(Winit.VirtualKeyCode.F14)] F14,
    [EnumMapTo(Winit.VirtualKeyCode.F15)] F15,
    [EnumMapTo(Winit.VirtualKeyCode.F16)] F16,
    [EnumMapTo(Winit.VirtualKeyCode.F17)] F17,
    [EnumMapTo(Winit.VirtualKeyCode.F18)] F18,
    [EnumMapTo(Winit.VirtualKeyCode.F19)] F19,
    [EnumMapTo(Winit.VirtualKeyCode.F20)] F20,
    [EnumMapTo(Winit.VirtualKeyCode.F21)] F21,
    [EnumMapTo(Winit.VirtualKeyCode.F22)] F22,
    [EnumMapTo(Winit.VirtualKeyCode.F23)] F23,
    [EnumMapTo(Winit.VirtualKeyCode.F24)] F24,
    [EnumMapTo(Winit.VirtualKeyCode.Snapshot)] Snapshot,
    [EnumMapTo(Winit.VirtualKeyCode.Scroll)] Scroll,
    [EnumMapTo(Winit.VirtualKeyCode.Pause)] Pause,
    [EnumMapTo(Winit.VirtualKeyCode.Insert)] Insert,
    [EnumMapTo(Winit.VirtualKeyCode.Home)] Home,
    [EnumMapTo(Winit.VirtualKeyCode.Delete)] Delete,
    [EnumMapTo(Winit.VirtualKeyCode.End)] End,
    [EnumMapTo(Winit.VirtualKeyCode.PageDown)] PageDown,
    [EnumMapTo(Winit.VirtualKeyCode.PageUp)] PageUp,
    [EnumMapTo(Winit.VirtualKeyCode.Left)] Left,
    [EnumMapTo(Winit.VirtualKeyCode.Up)] Up,
    [EnumMapTo(Winit.VirtualKeyCode.Right)] Right,
    [EnumMapTo(Winit.VirtualKeyCode.Down)] Down,
    [EnumMapTo(Winit.VirtualKeyCode.Back)] Back,
    [EnumMapTo(Winit.VirtualKeyCode.Return)] Return,
    [EnumMapTo(Winit.VirtualKeyCode.Space)] Space,
    [EnumMapTo(Winit.VirtualKeyCode.Compose)] Compose,
    [EnumMapTo(Winit.VirtualKeyCode.Caret)] Caret,
    [EnumMapTo(Winit.VirtualKeyCode.Numlock)] Numlock,
    [EnumMapTo(Winit.VirtualKeyCode.Numpad0)] Numpad0,
    [EnumMapTo(Winit.VirtualKeyCode.Numpad1)] Numpad1,
    [EnumMapTo(Winit.VirtualKeyCode.Numpad2)] Numpad2,
    [EnumMapTo(Winit.VirtualKeyCode.Numpad3)] Numpad3,
    [EnumMapTo(Winit.VirtualKeyCode.Numpad4)] Numpad4,
    [EnumMapTo(Winit.VirtualKeyCode.Numpad5)] Numpad5,
    [EnumMapTo(Winit.VirtualKeyCode.Numpad6)] Numpad6,
    [EnumMapTo(Winit.VirtualKeyCode.Numpad7)] Numpad7,
    [EnumMapTo(Winit.VirtualKeyCode.Numpad8)] Numpad8,
    [EnumMapTo(Winit.VirtualKeyCode.Numpad9)] Numpad9,
    [EnumMapTo(Winit.VirtualKeyCode.NumpadAdd)] NumpadAdd,
    [EnumMapTo(Winit.VirtualKeyCode.NumpadDivide)] NumpadDivide,
    [EnumMapTo(Winit.VirtualKeyCode.NumpadDecimal)] NumpadDecimal,
    [EnumMapTo(Winit.VirtualKeyCode.NumpadComma)] NumpadComma,
    [EnumMapTo(Winit.VirtualKeyCode.NumpadEnter)] NumpadEnter,
    [EnumMapTo(Winit.VirtualKeyCode.NumpadEquals)] NumpadEquals,
    [EnumMapTo(Winit.VirtualKeyCode.NumpadMultiply)] NumpadMultiply,
    [EnumMapTo(Winit.VirtualKeyCode.NumpadSubtract)] NumpadSubtract,
    [EnumMapTo(Winit.VirtualKeyCode.AbntC1)] AbntC1,
    [EnumMapTo(Winit.VirtualKeyCode.AbntC2)] AbntC2,
    [EnumMapTo(Winit.VirtualKeyCode.Apostrophe)] Apostrophe,
    [EnumMapTo(Winit.VirtualKeyCode.Apps)] Apps,
    [EnumMapTo(Winit.VirtualKeyCode.Asterisk)] Asterisk,
    [EnumMapTo(Winit.VirtualKeyCode.At)] At,
    [EnumMapTo(Winit.VirtualKeyCode.Ax)] Ax,
    [EnumMapTo(Winit.VirtualKeyCode.Backslash)] Backslash,
    [EnumMapTo(Winit.VirtualKeyCode.Calculator)] Calculator,
    [EnumMapTo(Winit.VirtualKeyCode.Capital)] Capital,
    [EnumMapTo(Winit.VirtualKeyCode.Colon)] Colon,
    [EnumMapTo(Winit.VirtualKeyCode.Comma)] Comma,
    [EnumMapTo(Winit.VirtualKeyCode.Convert)] Convert,
    [EnumMapTo(Winit.VirtualKeyCode.Equals)] Equals,
    [EnumMapTo(Winit.VirtualKeyCode.Grave)] Grave,
    [EnumMapTo(Winit.VirtualKeyCode.Kana)] Kana,
    [EnumMapTo(Winit.VirtualKeyCode.Kanji)] Kanji,
    [EnumMapTo(Winit.VirtualKeyCode.LAlt)] LAlt,
    [EnumMapTo(Winit.VirtualKeyCode.LBracket)] LBracket,
    [EnumMapTo(Winit.VirtualKeyCode.LControl)] LControl,
    [EnumMapTo(Winit.VirtualKeyCode.LShift)] LShift,
    [EnumMapTo(Winit.VirtualKeyCode.LWin)] LWin,
    [EnumMapTo(Winit.VirtualKeyCode.Mail)] Mail,
    [EnumMapTo(Winit.VirtualKeyCode.MediaSelect)] MediaSelect,
    [EnumMapTo(Winit.VirtualKeyCode.MediaStop)] MediaStop,
    [EnumMapTo(Winit.VirtualKeyCode.Minus)] Minus,
    [EnumMapTo(Winit.VirtualKeyCode.Mute)] Mute,
    [EnumMapTo(Winit.VirtualKeyCode.MyComputer)] MyComputer,
    [EnumMapTo(Winit.VirtualKeyCode.NavigateForward)] NavigateForward,
    [EnumMapTo(Winit.VirtualKeyCode.NavigateBackward)] NavigateBackward,
    [EnumMapTo(Winit.VirtualKeyCode.NextTrack)] NextTrack,
    [EnumMapTo(Winit.VirtualKeyCode.NoConvert)] NoConvert,
    [EnumMapTo(Winit.VirtualKeyCode.OEM102)] OEM102,
    [EnumMapTo(Winit.VirtualKeyCode.Period)] Period,
    [EnumMapTo(Winit.VirtualKeyCode.PlayPause)] PlayPause,
    [EnumMapTo(Winit.VirtualKeyCode.Plus)] Plus,
    [EnumMapTo(Winit.VirtualKeyCode.Power)] Power,
    [EnumMapTo(Winit.VirtualKeyCode.PrevTrack)] PrevTrack,
    [EnumMapTo(Winit.VirtualKeyCode.RAlt)] RAlt,
    [EnumMapTo(Winit.VirtualKeyCode.RBracket)] RBracket,
    [EnumMapTo(Winit.VirtualKeyCode.RControl)] RControl,
    [EnumMapTo(Winit.VirtualKeyCode.RShift)] RShift,
    [EnumMapTo(Winit.VirtualKeyCode.RWin)] RWin,
    [EnumMapTo(Winit.VirtualKeyCode.Semicolon)] Semicolon,
    [EnumMapTo(Winit.VirtualKeyCode.Slash)] Slash,
    [EnumMapTo(Winit.VirtualKeyCode.Sleep)] Sleep,
    [EnumMapTo(Winit.VirtualKeyCode.Stop)] Stop,
    [EnumMapTo(Winit.VirtualKeyCode.Sysrq)] Sysrq,
    [EnumMapTo(Winit.VirtualKeyCode.Tab)] Tab,
    [EnumMapTo(Winit.VirtualKeyCode.Underline)] Underline,
    [EnumMapTo(Winit.VirtualKeyCode.Unlabeled)] Unlabeled,
    [EnumMapTo(Winit.VirtualKeyCode.VolumeDown)] VolumeDown,
    [EnumMapTo(Winit.VirtualKeyCode.VolumeUp)] VolumeUp,
    [EnumMapTo(Winit.VirtualKeyCode.Wake)] Wake,
    [EnumMapTo(Winit.VirtualKeyCode.WebBack)] WebBack,
    [EnumMapTo(Winit.VirtualKeyCode.WebFavorites)] WebFavorites,
    [EnumMapTo(Winit.VirtualKeyCode.WebForward)] WebForward,
    [EnumMapTo(Winit.VirtualKeyCode.WebHome)] WebHome,
    [EnumMapTo(Winit.VirtualKeyCode.WebRefresh)] WebRefresh,
    [EnumMapTo(Winit.VirtualKeyCode.WebSearch)] WebSearch,
    [EnumMapTo(Winit.VirtualKeyCode.WebStop)] WebStop,
    [EnumMapTo(Winit.VirtualKeyCode.Yen)] Yen,
    [EnumMapTo(Winit.VirtualKeyCode.Copy)] Copy,
    [EnumMapTo(Winit.VirtualKeyCode.Paste)] Paste,
    [EnumMapTo(Winit.VirtualKeyCode.Cut)] Cut,
}
