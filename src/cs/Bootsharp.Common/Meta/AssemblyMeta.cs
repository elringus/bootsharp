namespace Bootsharp;

/// <summary>
/// Bootsharp-specific metadata of a C# assembly included in compiled solution.
/// </summary>
/// <param name="Name">Name of the assembly; equals file name, including ".dll" extension.</param>
/// <param name="Bytes">Raw binary content of the assembly.</param>
public record AssemblyMeta (string Name, byte[] Bytes);
