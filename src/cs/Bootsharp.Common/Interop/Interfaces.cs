using System.Diagnostics.CodeAnalysis;

namespace Bootsharp;

/// <summary>
/// Provides access to generated interop types for interfaces supplied
/// under <see cref="JSExportAttribute"/> and <see cref="JSImportAttribute"/>.
/// </summary>
/// <remarks>
/// Exported interfaces are C# APIs invoked in JavaScript. Their C# implementation
/// (handler) is assumed to be supplied via <see cref="ExportInterface.Factory"/>
/// on program boot (usually via DI), before associated APIs are accessed in JavaScript.
/// Imported interfaces are JavaScript APIs invoked in C#. Their implementation
/// is instantiated in generated code and is available before program start.
/// </remarks>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
public static class Interfaces
{
    /// <summary>
    /// Interop classes generated for <see cref="JSExportAttribute"/> interfaces
    /// mapped by the generated class type. Expected to have <see cref="ExportInterface.Factory"/>
    /// invoked with the interface implementation (handler) before associated API usage in JS.
    /// </summary>
    public static IReadOnlyDictionary<Type, ExportInterface> Exports => exports;
    /// <summary>
    /// Implementations generated for <see cref="JSImportAttribute"/> interop
    /// interfaces mapped by the interface type of the associated implementation.
    /// </summary>
    public static IReadOnlyDictionary<Type, ImportInterface> Imports => imports;

    private static readonly Dictionary<Type, ExportInterface> exports = new();
    private static readonly Dictionary<Type, ImportInterface> imports = new();

    /// <summary>
    /// Maps type of the generated export interop class to the associated metadata.
    /// Invoked by the generated code before program start.
    /// </summary>
    public static void Register (Type @class, ExportInterface export) => exports[@class] = export;
    /// <summary>
    /// Maps interface type of the generated import implementation to the associated metadata.
    /// Invoked by the generated code before program start.
    /// </summary>
    public static void Register (Type @interface, ImportInterface import) => imports[@interface] = import;
}
