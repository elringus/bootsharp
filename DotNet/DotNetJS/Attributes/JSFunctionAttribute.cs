using System;

namespace DotNetJS;

/// <summary>
/// Applied to a partial method to bind it with a JavaScript function.
/// The implementation is expected to be assigned as 'dotnet.Namespace.Method = function'.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class JSFunctionAttribute : Attribute { }
