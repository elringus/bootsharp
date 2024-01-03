namespace Bootsharp;

/// <summary>
/// Inspected assembly.
/// </summary>
/// <param name="Name">Name of the assembly; equals file name, including ".dll" extension.</param>
/// <param name="Bytes">Raw content of the assembly.</param>
public record Assembly (string Name, byte[] Bytes);
