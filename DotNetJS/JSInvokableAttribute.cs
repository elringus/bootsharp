using System;

namespace DotNetJS
{
    /// <summary>
    /// Applied to a static method to expose it to JavaScript.
    /// The associated JavaScript function will be accessible
    /// via imported library object as 'lib.ClassName.MethodName'.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class JSInvokableAttribute : Attribute { }
}
