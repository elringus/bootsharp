using System;

namespace Bootsharp.Packer;

/// <summary>
/// Exception thrown from Bootsharp.Packer internal behaviour.
/// </summary>
internal sealed class Error(string message) : Exception(message);
