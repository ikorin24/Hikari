#nullable enable
using System;
using System.Diagnostics;

namespace Elffy;

public sealed class Mouse
{
    private readonly IHostScreen _screen;
    private Vector2 _pos;

    private bool _onScreen;
    private Vector2 _wheelDelta;

    /// <summary>Get whether the mouse is on the screen or not.</summary>
    public bool OnScreen => _onScreen;

    /// <summary>Get position of the mouse on the screen based on top-left.</summary>
    public Vector2 Position => _pos;

    public Vector2 PositionDelta => throw new NotImplementedException();

    /// <summary>Get wheel value difference from previouse frame.</summary>
    public float WheelDelta => throw new NotImplementedException();

    internal Mouse(IHostScreen screen)
    {
        _screen = screen;
    }

    internal void OnWheel(Vector2 delta)
    {
        _wheelDelta = delta;
    }

    internal void OnCursorMoved(Vector2 pos)
    {
        _pos = pos;
        Debug.WriteLine(pos);
    }

    internal void OnCursorEnteredLeft(bool entered)
    {
        _onScreen = entered;
        if(entered) {
            Debug.WriteLine("cursor entered");
        }
        else {
            Debug.WriteLine("curosr left");
        }
    }
}
