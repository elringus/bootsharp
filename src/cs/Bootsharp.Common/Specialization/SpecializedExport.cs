namespace Bootsharp;

/// <summary>
/// Base class for specialized exports annotated with <see cref="SpecializeExportAttribute"/>.
/// </summary>
/// /// <remarks>
/// Export specializer (the class inherited from this base) is expected to implement all the abstract
/// members specified on the import specializer.
/// </remarks>
/// <param name="it">An instance of the specialized exported type.</param>
public abstract class SpecializedExport (object it)
{
    protected internal readonly object _it = it;
}
