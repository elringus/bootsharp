namespace Bootsharp;

/// <summary>
/// Metadata about generated implementation for module supplied under <see cref="ImportAttribute"/>.
/// </summary>
/// <param name="Instance">Imported module implementation instance.</param>
public record ImportModule (object Instance);
