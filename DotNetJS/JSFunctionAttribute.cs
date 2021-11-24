using System;

namespace DotNetJS
{
    /// <summary>
    /// Applied to a partial method that is expected to be implemented in a global JavaScript function.
    /// Name of the JavaScript function is expected to be equal to the method name, but with first letter lower-cased.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class JSFunctionAttribute : Attribute { }
}
