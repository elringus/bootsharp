namespace Bootsharp.Publish;

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
