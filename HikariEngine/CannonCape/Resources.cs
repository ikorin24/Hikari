using System;

namespace CannonCape;

public static class Resources
{
    private readonly static string _root;

    static Resources()
    {
        _root = System.IO.Path.Combine(AppContext.BaseDirectory, "resources/");
    }

    public static string Path(string name)
    {
        return $"{_root}{name}";
    }
}
