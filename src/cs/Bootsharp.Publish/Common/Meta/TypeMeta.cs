namespace Bootsharp.Publish;

/// <summary>
/// Describes a CLR type that either crosses the interop boundary directly, or is referenced by such a type.
/// </summary>
internal record TypeMeta (Type Clr)
{
    /// <summary>
    /// The described CLR type.
    /// </summary>
    public Type Clr { get; } = Clr;
    /// <summary>
    /// Fully qualified C# syntax of the type.
    /// </summary>
    public string Syntax { get; } = BuildSyntax(Clr);
}
