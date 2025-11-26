namespace Bootsharp;

/// <summary>
/// Applied to a partial method to bind it with an event meant to be
/// broadcast (invoked) in C# and subscribed (listened) to in JavaScript.
/// </summary>
/// <example>
/// <code>
/// [JSEvent]
/// public static partial void OnSomethingHappened (string payload);
/// Namespace.onSomethingHappened.subscribe(payload => ...);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class JSEventAttribute : Attribute;
