namespace CannonCape;

public static class Resources
{
    private const string Root = "resources/";

    public static string Path(string name)
    {
        return $"{Root}{name}";
    }
}
