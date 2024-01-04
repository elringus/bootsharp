namespace Bootsharp;

/// <summary>
/// Bootsharp-specific metadata of an interop method.
/// </summary>
public record MethodMeta
{
    /// <summary>
    /// Type of interop the method is implementing.
    /// </summary>
    public required MethodType Type { get; init; }
    /// <summary>
    /// C# assembly name (DLL file name, without the extension), under which the method is defined.
    /// </summary>
    public required string Assembly { get; init; }
    /// <summary>
    /// Full name of the C# type (including namespace), under which the method is defined.
    /// </summary>
    public required string DeclaringName { get; init; }
    /// <summary>
    /// JavaScript object(s) name under which the method (function) will be defined; resolved
    /// from C# namespace after applying user-defined namespace converters.
    /// </summary>
    public required string JSSpace { get; init; }
    /// <summary>
    /// C# name of the method, as specified in source code.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// JavaScript name of the method (function), as will be specified in source code.
    /// </summary>
    public required string JSName { get; init; }
    /// <summary>
    /// Arguments of the method.
    /// </summary>
    public required IReadOnlyList<ArgumentMeta> Arguments { get; init; }
    /// <summary>
    /// Metadata of the type returned by the method.
    /// </summary>
    public required TypeMeta ReturnType { get; init; }

    public override string ToString ()
    {
        var args = string.Join(", ", Arguments.Select(a => a.ToString()));
        return $"[{Type}] {Assembly}.{DeclaringName}.{Name} ({args}) => {ReturnType.JSSyntax}";
    }
}
