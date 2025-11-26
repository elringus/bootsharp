namespace Bootsharp;

/// <summary>
/// Exception thrown from Bootsharp internal behaviour.
/// </summary>
public sealed class Error (string message) : Exception(message);
