#nullable enable
using Elffy.Bind;
using System;

namespace Elffy;

internal sealed class HostScreen : IHostScreen
{
    private Box<CE.HostScreen> _screen;
    private CE.HostScreenId _id;

    public event Action<IHostScreen, uint, uint>? Resized;
    public event Action<IHostScreen>? RedrawRequested;


    public Ref<CE.HostScreen> Screen => _screen;
    public nuint Id => _id.AsNumber();

    internal HostScreen(Box<CE.HostScreen> screen, CE.HostScreenId id)
    {
        _screen = screen;
        _id = id;
    }

    internal void OnInitialize()
    {

    }

    internal void OnCleared()
    {
        _screen.AsRef().ScreenRequestRedraw();
    }

    internal void OnRedrawRequested()
    {
        // TODO:
    }

    internal void OnResized(uint width, uint height)
    {
        Resized?.Invoke(this, width, height);
    }
}

public interface IHostScreen
{
    event Action<IHostScreen, uint, uint>? Resized;
    nuint Id { get; }
}
