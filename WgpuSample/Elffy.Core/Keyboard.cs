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

    internal bool IsDown(Winit.VirtualKeyCode key)
    {
        var index = (u32)key;
        return
            _prevState[index] == false &&
            _currentAction[index].HasFlag(KeyActionFlag.Pressed);
    }

    internal bool IsUp(Winit.VirtualKeyCode key)
    {
        var index = (u32)key;
        return
            _prevState[index] == true &&
            _currentAction[index].HasFlag(KeyActionFlag.Released);
    }

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
