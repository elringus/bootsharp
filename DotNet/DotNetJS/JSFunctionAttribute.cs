using System;

namespace DotNetJS
{
    /// <summary>
    /// Applied to a partial method to bind it with a JavaScript function.
    /// The function implementation is expected to be assigned as
    /// 'dotnet.Assembly.Method = function' before the library is booted.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class JSFunctionAttribute : Attribute { }
}
