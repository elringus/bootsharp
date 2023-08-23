namespace Bootsharp;

/// <summary>
/// Provides access to JavaScript events via interop-specific endpoints.
/// </summary>
/// <remarks>
/// Arguments are expected to be JSON-serializable.
/// </remarks>
public static partial class Event
{
    /// <summary>
    /// Invokes JavaScript event with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the event to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the event or null when invoking w/o arguments.</param>
    [System.Runtime.InteropServices.JavaScript.JSImport("broadcast", "Bootsharp")]
    public static partial void Broadcast (string endpoint, string[]? args = null);
}
