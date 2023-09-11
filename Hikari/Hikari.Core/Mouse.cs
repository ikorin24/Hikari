#nullable enable
using System.Runtime.CompilerServices;
using Hikari.NativeBind;

namespace Hikari;

public sealed class Mouse
{
    private readonly Screen _screen;
    private PosBuf _posBuf;
    private readonly KeyBuf[] _namedKeys = new KeyBuf[3];
    private bool _areAnyButtonsChanged;
    private readonly object _sync = new();

    private bool _isOnScreen;
    private WheelDeltaBuf _wheelDeltaBuf;

    public Screen Screen => _screen;

    public bool IsOnScreen => _isOnScreen;

    internal bool AreAnyButtonsChanged
    {
        get
        {
            lock(_sync) {
                return _areAnyButtonsChanged;
            }
        }
    }

    public Vector2? Position
    {
        get
        {
            lock(_sync) {
                return _posBuf.Current;
            }
        }
    }

    public Vector2? PositionDelta
    {
        get
        {
            lock(_sync) {
                return _posBuf.Delta;
            }
        }
    }

    public float WheelDelta
    {
        get
        {
            lock(_sync) {
                return _wheelDeltaBuf.Current.Y;
            }
        }
    }

    internal Mouse(Screen screen)
    {
        _screen = screen;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsChanged(MouseButton button)
    {
        lock(_sync) {
            ref readonly var key = ref _namedKeys[(uint)button];
            return key.IsKeyUp || key.IsKeyDown;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsDown(MouseButton button)
    {
        lock(_sync) {
            return _namedKeys[(uint)button].IsKeyDown;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsPressed(MouseButton button)
    {
        lock(_sync) {
            return _namedKeys[(uint)button].IsKeyPressed;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsUp(MouseButton button)
    {
        lock(_sync) {
            return _namedKeys[(uint)button].IsKeyUp;
        }
    }

    internal void OnMouseButton(CH.MouseButton button, bool pressed)
    {
        if(button.is_named_buton) {
            lock(_sync) {
                _namedKeys[button.number].SetValue(pressed);
            }
            _areAnyButtonsChanged = true;
        }
        else {
            // nop
        }
    }

    internal void OnWheel(Vector2 delta)
    {
        lock(_sync) {
            _wheelDeltaBuf.SetValue(delta);
        }
    }

    internal void OnCursorMoved(Vector2 pos)
    {
        lock(_sync) {
            _posBuf.SetValue(pos);
        }
    }

    internal void OnCursorEnteredLeft(bool entered)
    {
        lock(_sync) {
            _isOnScreen = entered;
            if(entered == false) {
                _posBuf.SetValue(null);
            }
        }
    }

    internal void InitFrame()
    {
        lock(_sync) {
            _posBuf.InitFrame();
            _wheelDeltaBuf.InitFrame();
            for(int i = 0; i < _namedKeys.Length; i++) {
                _namedKeys[i].InitFrame();
            }
        }
    }

    internal void PrepareNextFrame()
    {
        _areAnyButtonsChanged = false;
    }

    private struct PosBuf
    {
        private Vector2? _delta;
        private Vector2? _current;
        private Vector2? _newValue;
        private bool _isNewValueSet;

        public readonly Vector2? Current => _current;

        public readonly Vector2? Delta => _delta;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(Vector2? value)
        {
            _newValue = value;
            _isNewValueSet = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InitFrame()
        {
            if(_isNewValueSet) {
                _delta = _newValue - _current;
                _current = _newValue;
            }
            else {
                _delta = default;
            }
            _isNewValueSet = false;
        }
    }

    private struct KeyBuf
    {
        private bool _current;
        private bool _prev;
        private bool _isNewValueSet;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(bool value)
        {
            _prev = _current;
            _current = value;
            _isNewValueSet = true;
        }

        public readonly bool IsKeyPressed => _current;

        public readonly bool IsKeyDown => _current && !_prev;

        public readonly bool IsKeyUp => !_current && _prev;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InitFrame()
        {
            if(_isNewValueSet == false) {
                _prev = _current;
            }
            _isNewValueSet = false;
        }
    }

    private struct WheelDeltaBuf
    {
        private Vector2 _current;
        private Vector2 _newValue;
        private bool _isNewValueSet;

        public readonly Vector2 Current => _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(Vector2 value)
        {
            _newValue = value;
            _isNewValueSet = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InitFrame()
        {
            if(_isNewValueSet) {
                _current = _newValue;
            }
            else {
                _current = default;
            }
            _isNewValueSet = false;
        }
    }
}

public enum MouseButton : uint
{
    Left = 0,
    Right,
    Middle,
}
