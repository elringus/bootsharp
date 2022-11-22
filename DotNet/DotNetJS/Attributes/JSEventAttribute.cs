using System;

namespace DotNetJS;

/// <summary>
/// Applied to a partial method to bind it with an event meant to be
/// broadcast (invoked) in C# and subscribed (listened) to in JavaScript.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class JSEventAttribute : Attribute { }
