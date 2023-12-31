using Bootsharp;

Log("Hello from .NET!");

public static partial class Program
{
    [JSFunction]
    public static partial void Log (string message);
}
