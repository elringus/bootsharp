namespace Bootsharp;

/// <summary>
/// Allows customizing the way Bootsharp treats specified CLR type on the export direction (C# -> JS).
/// When applied to a class inherited from <see cref="SpecializedExport"/>, Bootsharp wraps exported
/// instances of the specialized type into instances of this class before crossing the interop boundary.
/// The exported specialization is expected to be paired with the <see cref="SpecializeImportAttribute"/>
/// counterpart and contain implementations for all the abstract members defined on the imported specialization.
/// </summary>
/// <param name="Clr">
/// The CLR type to specialize the export for.
/// When the type is a class, will specialize subclasses as well.
/// </param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SpecializeExportAttribute (Type Clr) : Attribute
{
    public Type Clr { get; } = Clr;
}
