using System;

namespace Bootsharp.Builder;

/// <summary>
/// Exception thrown from Bootsharp.Builder internal behaviour.
/// </summary>
internal sealed class Error(string message) : Exception(message);
