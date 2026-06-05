namespace Bootsharp;

/// <summary>
/// Allows customizing the way Bootsharp treats specified CLR type on the import direction (JS -> C#).
/// When applied to an abstract class inherited from <see cref="SpecializedImport"/>, Bootsharp will use
/// the class as the base for the generated interop proxy and treat the abstract members of the class as
/// the actual interop surface. All the abstract members are expected to be implemented on the paired export
/// specialization of the class annotated with <see cref="SpecializeExportAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SpecializeImportAttribute (Type clr, string? CS = null, string? JS = null,
    string? JSCtor = null, string? Decl = null) : Attribute
{
    /// <summary>
    /// The CLR type to specialize the import for.
    /// </summary>
    public Type Clr { get; } = clr;
    /// <summary>
    /// Raw snippet spliced into the generated C# import proxy class body.
    /// </summary>
    public string? CS { get; } = CS;
    /// <summary>
    /// Raw snippet spliced into the generated JavaScript export proxy class body.
    /// </summary>
    public string? JS { get; } = JS;
    /// <summary>
    /// Raw snippet spliced into the generated JavaScript export proxy constructor.
    /// </summary>
    public string? JSCtor { get; } = JSCtor;
    /// <summary>
    /// Raw snippet spliced into the generated TypeScript declaration.
    /// When starts with 'export ' will instead replace the whole declaration.
    /// </summary>
    public string? Decl { get; } = Decl;
}
