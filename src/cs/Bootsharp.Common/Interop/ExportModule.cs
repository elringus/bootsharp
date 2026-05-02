namespace Bootsharp;

/// <summary>
/// Metadata about generated interop class for a module supplied under <see cref="ExportAttribute"/>.
/// </summary>
/// <param name="Handler">Type of the exported module's handler (interface or class).</param>
/// <param name="Factory">Takes export module handler instance; returns interop class instance.</param>
public record ExportModule (Type Handler, Func<object, object> Factory);
