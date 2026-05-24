namespace Bootsharp;

/// <summary>
/// Base class for specialized imports annotated with <see cref="SpecializeImportAttribute"/>.
/// </summary>
/// <remarks>
/// Import specializer (the class inherited from this base) is expected to either implement the specialized
/// interface, or (when the specialized type is not an interface) override <see cref="Unwrap"/> to return
/// the instance or value (in case of value types) of the expected concrete type.
/// </remarks>
/// <param name="id">Unique identifier of the specialized imported type instance.</param>
public abstract class SpecializedImport (int id) : JSProxy(id)
{
    /// <summary>
    /// Returns the instance or value of the specialized CLR type.
    /// By default, it's the proxy itself (which implements the specialized CLR type), however
    /// non-interface specializations override this to produce the object of the expected concrete type.
    /// </summary>
    protected internal virtual object Unwrap () => this;
}
