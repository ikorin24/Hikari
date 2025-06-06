using Hikari;

namespace CannonCape;

public sealed class Input
{
    private readonly Keyboard _keyboard;

    public Input(Screen screen)
    {
        _keyboard = screen.Keyboard;
    }

    public bool IsArrowLeftPressed()
    {
        return _keyboard.IsPressed(KeyCode.KeyA) || _keyboard.IsPressed(KeyCode.ArrowLeft);
    }

    public bool IsArrowRightPressed()
    {
        return _keyboard.IsPressed(KeyCode.KeyD) || _keyboard.IsPressed(KeyCode.ArrowRight);
    }

    public bool IsArrowUpPressed()
    {
        return _keyboard.IsPressed(KeyCode.KeyW) || _keyboard.IsPressed(KeyCode.ArrowUp);
    }

    public bool IsArrowDownPressed()
    {
        return _keyboard.IsPressed(KeyCode.KeyS) || _keyboard.IsPressed(KeyCode.ArrowDown);
    }

    public bool IsOkDown()
    {
        return _keyboard.IsDown(KeyCode.Space);
    }
}
