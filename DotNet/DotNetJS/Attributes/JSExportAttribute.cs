using System;

namespace DotNetJS;

/// <summary>
/// When applied to WASM entry point assembly, specified interfaces will
/// be automatically exported for consumption on JavaScript side.
/// </summary>
/// <remarks>
/// Generated bindings have to be initialized with the handler implementation.
/// For example, given 'IHandler' interface is exported, 'JSHandler' class will be generated,
/// which has to be instantiated with an 'IHandler' implementation instance.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class JSExportAttribute : JSTypeAttribute
{
    /// <inheritdoc />
    public JSExportAttribute (Type[] types,
        string? namePattern = null, string? nameReplacement = null,
        string? invokePattern = null, string? invokeReplacement = null)
        : base(types, namePattern, nameReplacement, invokePattern, invokeReplacement) { }
}
