namespace Bootsharp.Publish;

/// <summary>
/// Method declared on either <see cref="JSExportAttribute"/> or
/// <see cref="JSImportAttribute"/> interface.
/// </summary>
internal sealed record InterfaceMethodMeta
{
    /// <summary>
    /// Name of the method as declared on the interface.
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// Metadata about the interop method generated for the interface method.
    /// </summary>
    public required MethodMeta Generated { get; set; }
}
