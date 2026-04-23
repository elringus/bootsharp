using System;
using Bootsharp;

public static partial class Program
{
    [Export] // Used in JS as Program.onMainInvoked.subscribe(...)
    public static event Action<string>? OnMainInvoked;

    public static void Main ()
    {
        OnMainInvoked?.Invoke($"Hello {GetFrontendName()}, .NET here!");
    }

    [Import] // Assigned in JS as Program.getFrontendName = ...
    public static partial string GetFrontendName ();

    [Export] // Invoked from JS as Program.GetBackendName()
    public static string GetBackendName () => $".NET {Environment.Version}";
}
