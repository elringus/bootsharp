namespace Bootsharp;

/// <summary>
/// Allows customizing the way Bootsharp treats specified CLR type on the export direction (C# -> JS).
/// When applied to a class inherited from <see cref="SpecializedExport"/>, Bootsharp wraps exported
/// instances of the specialized type into instances of this class before crossing the interop boundary.
/// The exported specialization is expected to be paired with the <see cref="SpecializeImportAttribute"/>
/// counterpart and contain implementations for all the abstract members defined on the imported specialization.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SpecializeExportAttribute (Type clr) : Attribute
{
    /// <summary>
    /// The CLR type to specialize the export for.
    /// </summary>
    public Type Clr { get; } = clr;
}
