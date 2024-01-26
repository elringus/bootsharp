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
    /// <remarks>
    /// <see cref="MethodMeta.Name"/> of the generated C# implementation may
    /// differ from the source method name on the interface, hence the wrapper.
    /// </remarks>
    public required string Name { get; set; }
    /// <summary>
    /// Metadata of the interop method.
    /// </summary>
    public required MethodMeta Meta { get; set; }
}
