#nullable enable

namespace Elffy.NativeBind;

internal static class Winit
{
    public enum VirtualKeyCode : u32
    {
        /// The '1' key over the letters.
        [EnumMapTo(Keys.Key1)] Key1,
        /// The '2' key over the letters.
        [EnumMapTo(Keys.Key2)] Key2,
        /// The '3' key over the letters.
        [EnumMapTo(Keys.Key3)] Key3,
        /// The '4' key over the letters.
        [EnumMapTo(Keys.Key4)] Key4,
        /// The '5' key over the letters.
        [EnumMapTo(Keys.Key5)] Key5,
        /// The '6' key over the letters.
        [EnumMapTo(Keys.Key6)] Key6,
        /// The '7' key over the letters.
        [EnumMapTo(Keys.Key7)] Key7,
        /// The '8' key over the letters.
        [EnumMapTo(Keys.Key8)] Key8,
        /// The '9' key over the letters.
        [EnumMapTo(Keys.Key9)] Key9,
        /// The '0' key over the 'O' and 'P' keys.
        [EnumMapTo(Keys.Key0)] Key0,

        [EnumMapTo(Keys.A)] A,
        [EnumMapTo(Keys.B)] B,
        [EnumMapTo(Keys.C)] C,
        [EnumMapTo(Keys.D)] D,
        [EnumMapTo(Keys.E)] E,
        [EnumMapTo(Keys.F)] F,
        [EnumMapTo(Keys.G)] G,
        [EnumMapTo(Keys.H)] H,
        [EnumMapTo(Keys.I)] I,
        [EnumMapTo(Keys.J)] J,
        [EnumMapTo(Keys.K)] K,
        [EnumMapTo(Keys.L)] L,
        [EnumMapTo(Keys.M)] M,
        [EnumMapTo(Keys.N)] N,
        [EnumMapTo(Keys.O)] O,
        [EnumMapTo(Keys.P)] P,
        [EnumMapTo(Keys.Q)] Q,
        [EnumMapTo(Keys.R)] R,
        [EnumMapTo(Keys.S)] S,
        [EnumMapTo(Keys.T)] T,
        [EnumMapTo(Keys.U)] U,
        [EnumMapTo(Keys.V)] V,
        [EnumMapTo(Keys.W)] W,
        [EnumMapTo(Keys.X)] X,
        [EnumMapTo(Keys.Y)] Y,
        [EnumMapTo(Keys.Z)] Z,

        /// The Escape key, next to F1.
        [EnumMapTo(Keys.Escape)] Escape,

        [EnumMapTo(Keys.F1)] F1,
        [EnumMapTo(Keys.F2)] F2,
        [EnumMapTo(Keys.F3)] F3,
        [EnumMapTo(Keys.F4)] F4,
        [EnumMapTo(Keys.F5)] F5,
        [EnumMapTo(Keys.F6)] F6,
        [EnumMapTo(Keys.F7)] F7,
        [EnumMapTo(Keys.F8)] F8,
        [EnumMapTo(Keys.F9)] F9,
        [EnumMapTo(Keys.F10)] F10,
        [EnumMapTo(Keys.F11)] F11,
        [EnumMapTo(Keys.F12)] F12,
        [EnumMapTo(Keys.F13)] F13,
        [EnumMapTo(Keys.F14)] F14,
        [EnumMapTo(Keys.F15)] F15,
        [EnumMapTo(Keys.F16)] F16,
        [EnumMapTo(Keys.F17)] F17,
        [EnumMapTo(Keys.F18)] F18,
        [EnumMapTo(Keys.F19)] F19,
        [EnumMapTo(Keys.F20)] F20,
        [EnumMapTo(Keys.F21)] F21,
        [EnumMapTo(Keys.F22)] F22,
        [EnumMapTo(Keys.F23)] F23,
        [EnumMapTo(Keys.F24)] F24,

        /// Print Screen/SysRq.
        [EnumMapTo(Keys.Snapshot)] Snapshot,
        /// Scroll Lock.
        [EnumMapTo(Keys.Scroll)] Scroll,
        /// Pause/Break key, next to Scroll lock.
        [EnumMapTo(Keys.Pause)] Pause,

        /// `Insert`, next to Backspace.
        [EnumMapTo(Keys.Insert)] Insert,
        [EnumMapTo(Keys.Home)] Home,
        [EnumMapTo(Keys.Delete)] Delete,
        [EnumMapTo(Keys.End)] End,
        [EnumMapTo(Keys.PageDown)] PageDown,
        [EnumMapTo(Keys.PageUp)] PageUp,

        [EnumMapTo(Keys.Left)] Left,
        [EnumMapTo(Keys.Up)] Up,
        [EnumMapTo(Keys.Right)] Right,
        [EnumMapTo(Keys.Down)] Down,

        /// The Backspace key, right over Enter.
        [EnumMapTo(Keys.Back)] Back,
        /// The Enter key.
        [EnumMapTo(Keys.Return)] Return,
        /// The space bar.
        [EnumMapTo(Keys.Space)] Space,

        /// The "Compose" key on Linux.
        [EnumMapTo(Keys.Compose)] Compose,

        [EnumMapTo(Keys.Caret)] Caret,

        [EnumMapTo(Keys.Numlock)] Numlock,
        [EnumMapTo(Keys.Numpad0)] Numpad0,
        [EnumMapTo(Keys.Numpad1)] Numpad1,
        [EnumMapTo(Keys.Numpad2)] Numpad2,
        [EnumMapTo(Keys.Numpad3)] Numpad3,
        [EnumMapTo(Keys.Numpad4)] Numpad4,
        [EnumMapTo(Keys.Numpad5)] Numpad5,
        [EnumMapTo(Keys.Numpad6)] Numpad6,
        [EnumMapTo(Keys.Numpad7)] Numpad7,
        [EnumMapTo(Keys.Numpad8)] Numpad8,
        [EnumMapTo(Keys.Numpad9)] Numpad9,
        [EnumMapTo(Keys.NumpadAdd)] NumpadAdd,
        [EnumMapTo(Keys.NumpadDivide)] NumpadDivide,
        [EnumMapTo(Keys.NumpadDecimal)] NumpadDecimal,
        [EnumMapTo(Keys.NumpadComma)] NumpadComma,
        [EnumMapTo(Keys.NumpadEnter)] NumpadEnter,
        [EnumMapTo(Keys.NumpadEquals)] NumpadEquals,
        [EnumMapTo(Keys.NumpadMultiply)] NumpadMultiply,
        [EnumMapTo(Keys.NumpadSubtract)] NumpadSubtract,

        [EnumMapTo(Keys.AbntC1)] AbntC1,
        [EnumMapTo(Keys.AbntC2)] AbntC2,
        [EnumMapTo(Keys.Apostrophe)] Apostrophe,
        [EnumMapTo(Keys.Apps)] Apps,
        [EnumMapTo(Keys.Asterisk)] Asterisk,
        [EnumMapTo(Keys.At)] At,
        [EnumMapTo(Keys.Ax)] Ax,
        [EnumMapTo(Keys.Backslash)] Backslash,
        [EnumMapTo(Keys.Calculator)] Calculator,
        [EnumMapTo(Keys.Capital)] Capital,
        [EnumMapTo(Keys.Colon)] Colon,
        [EnumMapTo(Keys.Comma)] Comma,
        [EnumMapTo(Keys.Convert)] Convert,
        [EnumMapTo(Keys.Equals)] Equals,
        [EnumMapTo(Keys.Grave)] Grave,
        [EnumMapTo(Keys.Kana)] Kana,
        [EnumMapTo(Keys.Kanji)] Kanji,
        [EnumMapTo(Keys.LAlt)] LAlt,
        [EnumMapTo(Keys.LBracket)] LBracket,
        [EnumMapTo(Keys.LControl)] LControl,
        [EnumMapTo(Keys.LShift)] LShift,
        [EnumMapTo(Keys.LWin)] LWin,
        [EnumMapTo(Keys.Mail)] Mail,
        [EnumMapTo(Keys.MediaSelect)] MediaSelect,
        [EnumMapTo(Keys.MediaStop)] MediaStop,
        [EnumMapTo(Keys.Minus)] Minus,
        [EnumMapTo(Keys.Mute)] Mute,
        [EnumMapTo(Keys.MyComputer)] MyComputer,
        [EnumMapTo(Keys.NavigateForward)] NavigateForward,
        [EnumMapTo(Keys.NavigateBackward)] NavigateBackward,
        [EnumMapTo(Keys.NextTrack)] NextTrack,
        [EnumMapTo(Keys.NoConvert)] NoConvert,
        [EnumMapTo(Keys.OEM102)] OEM102,
        [EnumMapTo(Keys.Period)] Period,
        [EnumMapTo(Keys.PlayPause)] PlayPause,
        [EnumMapTo(Keys.Plus)] Plus,
        [EnumMapTo(Keys.Power)] Power,
        [EnumMapTo(Keys.PrevTrack)] PrevTrack,
        [EnumMapTo(Keys.RAlt)] RAlt,
        [EnumMapTo(Keys.RBracket)] RBracket,
        [EnumMapTo(Keys.RControl)] RControl,
        [EnumMapTo(Keys.RShift)] RShift,
        [EnumMapTo(Keys.RWin)] RWin,
        [EnumMapTo(Keys.Semicolon)] Semicolon,
        [EnumMapTo(Keys.Slash)] Slash,
        [EnumMapTo(Keys.Sleep)] Sleep,
        [EnumMapTo(Keys.Stop)] Stop,
        [EnumMapTo(Keys.Sysrq)] Sysrq,
        [EnumMapTo(Keys.Tab)] Tab,
        [EnumMapTo(Keys.Underline)] Underline,
        [EnumMapTo(Keys.Unlabeled)] Unlabeled,
        [EnumMapTo(Keys.VolumeDown)] VolumeDown,
        [EnumMapTo(Keys.VolumeUp)] VolumeUp,
        [EnumMapTo(Keys.Wake)] Wake,
        [EnumMapTo(Keys.WebBack)] WebBack,
        [EnumMapTo(Keys.WebFavorites)] WebFavorites,
        [EnumMapTo(Keys.WebForward)] WebForward,
        [EnumMapTo(Keys.WebHome)] WebHome,
        [EnumMapTo(Keys.WebRefresh)] WebRefresh,
        [EnumMapTo(Keys.WebSearch)] WebSearch,
        [EnumMapTo(Keys.WebStop)] WebStop,
        [EnumMapTo(Keys.Yen)] Yen,
        [EnumMapTo(Keys.Copy)] Copy,
        [EnumMapTo(Keys.Paste)] Paste,
        [EnumMapTo(Keys.Cut)] Cut,
    }

}
