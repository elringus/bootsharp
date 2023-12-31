using System.Diagnostics.CodeAnalysis;

namespace Bootsharp;

/// <summary>
/// Stores registered JavaScript bindings.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
public static class BindingRegistry
{
    /// <summary>
    /// Registered export bindings mapped by the binding implementation (auto-generated) type.
    /// </summary>
    public static IReadOnlyDictionary<Type, ExportBinding> Exports => exports;
    /// <summary>
    /// Registered import bindings mapped by the imported API interface type.
    /// </summary>
    public static IReadOnlyDictionary<Type, ImportBinding> Imports => imports;

    private static readonly Dictionary<Type, ExportBinding> exports = new();
    private static readonly Dictionary<Type, ImportBinding> imports = new();

    /// <summary>
    /// Maps implementation type to export binding; used internally by the auto-generated code.
    /// </summary>
    public static void Register (Type impl, ExportBinding binding) => exports[impl] = binding;
    /// <summary>
    /// Maps imported interface type to import binding; used internally by the auto-generated code.
    /// </summary>
    public static void Register (Type api, ImportBinding binding) => imports[api] = binding;
}
