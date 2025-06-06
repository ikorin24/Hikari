using Hikari;

namespace CannonCape;

public sealed class Input
{
    private readonly Keyboard _keyboard;

    public Input(Screen screen)
    {
        _keyboard = screen.Keyboard;
    }

    public bool IsLeftPressed()
    {
        return _keyboard.IsPressed(KeyCode.KeyA) || _keyboard.IsPressed(KeyCode.ArrowLeft);
    }

    public bool IsRightPressed()
    {
        return _keyboard.IsPressed(KeyCode.KeyD) || _keyboard.IsPressed(KeyCode.ArrowRight);
    }

    public bool IsUpPressed()
    {
        return _keyboard.IsPressed(KeyCode.KeyW) || _keyboard.IsPressed(KeyCode.ArrowUp);
    }

    public bool IsDownPressed()
    {
        return _keyboard.IsPressed(KeyCode.KeyS) || _keyboard.IsPressed(KeyCode.ArrowDown);
    }
}
