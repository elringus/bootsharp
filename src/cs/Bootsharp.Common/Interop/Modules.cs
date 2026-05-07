namespace Bootsharp;

/// <summary>
/// Provides access to generated interop types for modules supplied
/// under <see cref="ExportAttribute"/> and <see cref="ImportAttribute"/>.
/// </summary>
/// <remarks>
/// Exported modules are C# APIs invoked in JavaScript. Their C# implementation
/// (handler) is assumed to be supplied via <see cref="ExportModule.Factory"/>
/// on program start (usually via DI), before associated APIs are accessed in JavaScript.
/// Imported modules are JavaScript APIs invoked in C#. Their implementation
/// is instantiated in generated code and is available before program start.
/// </remarks>
public static class Modules
{
    /// <summary>
    /// Export modules metadata generated for types (classes or interfaces) specified under
    /// <see cref="ExportAttribute"/> mapped by the generated wrapper type.
    /// </summary>
    public static IReadOnlyDictionary<Type, ExportModule> Exports => exports;
    /// <summary>
    /// Import modules metadata generated for interface types specified under
    /// <see cref="ImportAttribute"/> assembly attribute mapped by the specified interface type.
    /// </summary>
    public static IReadOnlyDictionary<Type, ImportModule> Imports => imports;

    private static readonly Dictionary<Type, ExportModule> exports = new();
    private static readonly Dictionary<Type, ImportModule> imports = new();

    /// <summary>
    /// Maps type of the generated export module wrapper to the associated export module metadata.
    /// Invoked by the generated code before program start.
    /// </summary>
    public static void Register (Type wrapper, ExportModule export) => exports[wrapper] = export;
    /// <summary>
    /// Maps interface type of the imported module to the associated import module metadata.
    /// Invoked by the generated code before program start.
    /// </summary>
    public static void Register (Type @interface, ImportModule import) => imports[@interface] = import;
}
