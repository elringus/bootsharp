using System.Runtime.CompilerServices;

namespace Bootsharp;

/// <summary>
/// Exception thrown when Bootsharp fails to intercept user-defined interop method.
/// </summary>
public sealed class NotIntercepted ([CallerMemberName] string name = "") :
    Exception($"Bootsharp failed to intercept '${name}'.");
