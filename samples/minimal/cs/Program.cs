using System;
using Bootsharp;

public static partial class Program
{
    public static void Main ()
    {
        OnMainInvoked($"Hello {GetFrontendName()}, .NET here!");
    }

    [JSEvent] // Used in JS as Program.onMainInvoked.subscribe(...)
    public static partial void OnMainInvoked (string message);

    [JSFunction] // Assigned in JS as Program.getFrontendName = ...
    public static partial string GetFrontendName ();

    [JSInvokable] // Invoked from JS as Program.GetBackendName()
    public static string GetBackendName () => $".NET {Environment.Version}";
}
