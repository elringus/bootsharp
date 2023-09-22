namespace Bootsharp;

/// <summary>
/// Exported JavaScript binding.
/// </summary>
/// <param name="Api">Type of the exported interface.</param>
/// <param name="Factory">Binding's implementation factory function.</param>
public record ExportBinding(Type Api, Func<object, object> Factory);
