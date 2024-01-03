namespace Bootsharp;

/// <summary>
/// Inspected interop method.
/// </summary>
public record Method
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
    /// C# name of the method, as specified in source code.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// Arguments of the method.
    /// </summary>
    public required IReadOnlyList<Argument> Arguments { get; init; }
    /// <summary>
    /// C# type of the value returned by the method.
    /// </summary>
    public required Type ReturnType { get; init; }
    /// <summary>
    /// C# syntax of the return type, as specified in source code.
    /// </summary>
    public required string ReturnTypeSyntax { get; init; }
    /// <summary>
    /// Whether the method returns void.
    /// </summary>
    public required bool ReturnsVoid { get; init; }
    /// <summary>
    /// Whether the method returns optional/nullable value.
    /// </summary>
    public required bool ReturnsNullable { get; init; }
    /// <summary>
    /// Whether the method returns task/promise-like value.
    /// </summary>
    public required bool ReturnsTaskLike { get; init; }
    /// <summary>
    /// Whether the method's return value type has to be serialized for interop.
    /// </summary>
    public required bool ShouldSerializeReturnType { get; init; }
    /// <summary>
    /// JavaScript object name under which the method (function) will be defined.
    /// </summary>
    public required string JSSpace { get; init; }
    /// <summary>
    /// JavaScript (TypeScript) syntax of the return value type, as will
    /// be specified in source code.
    /// </summary>
    public required string JSReturnTypeSyntax { get; init; }

    public override string ToString ()
    {
        var args = string.Join(", ", Arguments.Select(a => a.ToString()));
        return $"[{Type}] {Assembly}.{DeclaringName}.{Name} ({args}) => {ReturnTypeSyntax}";
    }
}
