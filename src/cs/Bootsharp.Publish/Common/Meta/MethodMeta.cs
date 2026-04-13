namespace Bootsharp.Publish;

/// <summary>
/// Interop method.
/// </summary>
internal sealed record MethodMeta
{
    /// <summary>
    /// Type of interop the method is implementing.
    /// </summary>
    public required MethodKind Kind { get; init; }
    /// <summary>
    /// C# assembly name (DLL file name, w/o the extension), under which the method is declared.
    /// </summary>
    public required string Assembly { get; init; }
    /// <summary>
    /// Full name of the C# type (including namespace), under which the method is declared.
    /// </summary>
    public required string Space { get; init; }
    /// <summary>
    /// JavaScript object name(s) (joined with dot when nested) under which the associated interop
    /// function will be declared; resolved from <see cref="Space"/> with user-defined converters.
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
    /// When the method's class is a generated implementation of an interop interface, contains
    /// name of the associated interface method. The name may differ from <see cref="Name"/>,
    /// which would be the name of the method on the generated interface implementation and is
    /// subject to <see cref="Preferences.Event"/> and <see cref="Preferences.Function"/>.
    /// </summary>
    public string? InterfaceName { get; init; }
    /// <summary>
    /// Arguments of the method, in declaration order.
    /// </summary>
    public required IReadOnlyList<ArgumentMeta> Arguments { get; init; }
    /// <summary>
    /// Metadata of the value returned by the method.
    /// </summary>
    public required ValueMeta ReturnValue { get; init; }
    /// <summary>
    /// Whether the <see cref="ReturnValue"/> is void.
    /// </summary>
    public required bool Void { get; init; }
    /// <summary>
    /// Whether the <see cref="ReturnValue"/> is task-like (can be awaited).
    /// </summary>
    public required bool Async { get; init; }
}

/// <summary>
/// Type of interop method.
/// </summary>
internal enum MethodKind
{
    /// <summary>
    /// The method is implemented in C# and invoked from JavaScript;
    /// implementation has <see cref="JSInvokableAttribute"/>.
    /// </summary>
    Invokable,
    /// <summary>
    /// The method is implemented in JavaScript and invoked from C#;
    /// implementation has <see cref="JSFunctionAttribute"/>.
    /// </summary>
    Function,
    /// <summary>
    /// The method is invoked from C# to notify subscribers in JavaScript;
    /// implementation has <see cref="JSEventAttribute"/>.
    /// </summary>
    Event
}
