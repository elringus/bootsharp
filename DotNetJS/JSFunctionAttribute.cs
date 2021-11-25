using System;

namespace DotNetJS
{
    /// <summary>
    /// Applied to a static partial method to associate it with a JavaScript function.
    /// The implementing JavaScript function is expected to be assigned
    /// to the imported library object as 'lib.ClassName = { MethodName: function }'.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class JSFunctionAttribute : Attribute { }
}
