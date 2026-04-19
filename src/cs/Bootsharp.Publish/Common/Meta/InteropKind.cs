namespace Bootsharp.Publish;

/// <summary>
/// Direction of the interop boundary for a discovered API surface.
/// </summary>
internal enum InteropKind
{
    /// <summary>
    /// Implemented in C# and consumed from JavaScript.
    /// </summary>
    Export,
    /// <summary>
    /// Implemented in JavaScript and consumed from C#.
    /// </summary>
    Import
}
