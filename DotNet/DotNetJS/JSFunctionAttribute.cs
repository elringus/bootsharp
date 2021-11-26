using System;

namespace DotNetJS
{
    /// <summary>
    /// Applied to a partial method to associate it with JavaScript function.
    /// The implementing JavaScript function is expected to be assigned
    /// to the imported library object as 'lib.AssemblyName = { MethodName: function }'.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class JSFunctionAttribute : Attribute { }
}
