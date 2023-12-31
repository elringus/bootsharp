namespace Bootsharp;

/// <summary>
/// Applied to a partial method to bind it with a JavaScript function.
/// </summary>
/// <remarks>
/// The implementation is expected to be assigned as "Namespace.method = function".
/// </remarks>
/// <example>
/// <code>
/// [JSFunction]
/// public static partial string GetHostName ();
/// Namespace.getHostName = () => "Browser";
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class JSFunctionAttribute : Attribute;
