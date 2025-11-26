namespace Bootsharp;

/// <summary>
/// Metadata about generated interop class for an interface supplied
/// under <see cref="JSExportAttribute"/>.
/// </summary>
/// <param name="Interface">Type of the exported interface.</param>
/// <param name="Factory">Takes export interface implementation instance; returns interop class instance.</param>
public record ExportInterface (Type Interface, Func<object, object> Factory);
