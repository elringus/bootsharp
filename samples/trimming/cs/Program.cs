using Bootsharp;

public static partial class Program
{
    public static void Main ()
    {
        Log("Hello from .NET!");
    }

    [Import]
    public static partial void Log (string message);
}
