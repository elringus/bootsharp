using System;

namespace Bootsharp;

/// <summary>
/// Exception thrown from Bootsharp internal behaviour.
/// </summary>
internal sealed class Error(string message) : Exception(message);
