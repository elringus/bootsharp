using System;

namespace Bootsharp;

/// <summary>
/// Applied to a static method to make it invokable in JavaScript.
/// </summary>
/// <example>
/// <code>
/// [JSInvokable]
/// public static string GetName () => "Sharp";
/// console.log(Namespace.getName());
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class JSInvokableAttribute : Attribute;
