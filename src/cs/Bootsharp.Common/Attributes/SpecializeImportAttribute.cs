namespace Bootsharp;

/// <summary>
/// Allows customizing the way Bootsharp treats specified CLR type on the import direction (JS -> C#).
/// When applied to an abstract class inherited from <see cref="SpecializedImport"/>, Bootsharp will use
/// the class as the base for the generated interop proxy and treat the abstract members of the class as
/// the actual interop surface. All the abstract members are expected to be implemented on the paired export
/// specialization of the class annotated with <see cref="SpecializeExportAttribute"/>.
/// </summary>
/// <param name="Clr">
/// The CLR type to specialize the import for.
/// When the type is a class, will specialize subclasses as well.
/// </param>
/// <param name="CS">
/// Raw snippet spliced into the generated C# import proxy class body.
/// All occurrences of '$full' are replaced with the fully-qualified type name of the specialized instance type.
/// </param>
/// <param name="JS">
/// Raw snippet spliced into the generated JavaScript export proxy class body.
/// </param>
/// <param name="JSCtor">
/// Raw snippet spliced into the generated JavaScript export proxy constructor.
/// </param>
/// <param name="Decl">
/// Raw snippet spliced into the generated TypeScript declaration.
/// When starts with 'export ' will instead replace the whole declaration.
/// Occurrences of '$name' are replaced with the name of the specialized instance type.
/// Occurrences of '$full' are replaced with the fully-qualified name of the specialized instance type.
/// When the instance type is generic, occurrences of '$T{I}' are replaced with the fully-qualified names
/// of the generic type arguments with the {I} index, starting with 0 (eg, '$T0' is the first generic arg).
/// </param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SpecializeImportAttribute (Type Clr, string? CS = null, string? JS = null,
    string? JSCtor = null, string? Decl = null) : Attribute
{
    public Type Clr { get; } = Clr;
    public string? CS { get; } = CS;
    public string? JS { get; } = JS;
    public string? JSCtor { get; } = JSCtor;
    public string? Decl { get; } = Decl;
}
