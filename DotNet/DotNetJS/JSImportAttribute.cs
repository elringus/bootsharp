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
public sealed class JSImportAttribute : Attribute
{
    /// <summary>
    /// The types to import.
    /// </summary>
    public Type[] Types { get; }

    /// <param name="types">The types to import.</param>
    public JSImportAttribute (Type[] types)
    {
        Types = types;
    }
}
