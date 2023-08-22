using System;

namespace Bootsharp;

/// <summary>
/// Exception thrown from Bootsharp internal behaviour.
/// </summary>
public class Error(string message) : Exception(message);
