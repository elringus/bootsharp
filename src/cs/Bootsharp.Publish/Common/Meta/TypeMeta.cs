namespace Bootsharp.Publish;

/// <summary>
/// Describes a CLR type that crosses the interop boundary.
/// </summary>
internal sealed record TypeMeta (Type Clr)
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
