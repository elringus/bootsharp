using System;

namespace DotNetJS;

/// <summary>
/// When applied to WASM entry point assembly, JavaScript bindings for the specified interfaces
/// will be automatically generated for consumption on C# side.
/// </summary>
/// <remarks>
/// Generated bindings have to be implemented on JavaScript side.
/// For example, given 'IFrontend' interface is imported, 'JSFrontend' class will be generated,
/// which has to be implemented in JavaScript.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class JSImportAttribute : JSTypeAttribute
{
    /// <inheritdoc />
    public JSImportAttribute (Type[] types,
        string? namePattern = null, string? nameReplacement = null,
        string? invokePattern = null, string? invokeReplacement = null)
        : base(types, namePattern, nameReplacement, invokePattern, invokeReplacement) { }
}
