using static Bootsharp.Serializer;

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
    /// <param name="args">Arguments for the event.</param>
    public static void Broadcast (string endpoint, params object[] args) =>
        BroadcastSerialized(endpoint, SerializeArgs(args));

    [System.Runtime.InteropServices.JavaScript.JSImport("broadcast", "Bootsharp")]
    private static partial void BroadcastSerialized (string endpoint, string[]? args = null);
}
