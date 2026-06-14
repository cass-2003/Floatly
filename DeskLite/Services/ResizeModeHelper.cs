namespace DeskLite.Services;

public static class ResizeModeHelper
{
    public const string Uniform = "uniform";
    public const string Free = "free";

    public static string Normalize(string? mode) => mode switch
    {
        Free => Free,
        _ => Uniform
    };
}
