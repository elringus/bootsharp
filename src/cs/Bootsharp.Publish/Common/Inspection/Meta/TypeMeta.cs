namespace Bootsharp.Publish;

/// <summary>
/// Describes a C# CLR type that either crosses the interop boundary directly, or is otherwise associated
/// with such types; the derivatives are <see cref="SurfaceMeta"/> and <see cref="SerializedMeta"/>.
/// </summary>
internal record TypeMeta (Type Clr)
{
    /// <summary>
    /// The described CLR type.
    /// </summary>
    public Type Clr { get; } = Clr;
    /// <summary>
    /// Unique identifier of the type.
    /// </summary>
    public string Id { get; } = BuildId(Clr);
    /// <summary>
    /// Fully qualified C# syntax of the type.
    /// </summary>
    public string Syntax { get; } = BuildSyntax(Clr);
    /// <summary>
    /// The path to the module containing the node that represents the type in JavaScript.
    /// </summary>
    public string JSModule { get; } = Slugify(WithPref(Pref.Space, Clr.Namespace ?? "index"));
    /// <summary>
    /// The path to the node inside the module that represents the type in JavaScript.
    /// </summary>
    public string JSNode { get; } = WithPref(Pref.Name, TrimGeneric(Clr.FullName!).Split('.')[^1]).Replace('+', '.');
}
