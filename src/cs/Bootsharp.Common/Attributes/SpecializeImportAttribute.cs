namespace Bootsharp;

/// <summary>
/// Allows customizing the way Bootsharp treats specified CLR type on the import direction (JS -> C#).
/// When applied to an abstract class inherited from <see cref="SpecializedImport"/>, Bootsharp will use
/// the class as the base for the generated interop proxy and treat the abstract members of the class as
/// the actual interop surface. All the abstract members are expected to be implemented on the paired export
/// specialization of the class annotated with <see cref="SpecializeExportAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SpecializeImportAttribute (Type clr, string? JS = null, string? Decl = null) : Attribute
{
    /// <summary>
    /// The CLR type to specialize the import for.
    /// </summary>
    public Type Clr { get; } = clr;
    /// <summary>
    /// Raw snippet spliced into the generated JavaScript proxy class.
    /// </summary>
    public string? JS { get; } = JS;
    /// <summary>
    /// Raw snippet spliced into the generated TypeScript declaration.
    /// </summary>
    public string? Decl { get; } = Decl;
}
