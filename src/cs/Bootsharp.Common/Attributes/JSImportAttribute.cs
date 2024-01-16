namespace Bootsharp;

/// <summary>
/// When applied to WASM entry point assembly, JavaScript bindings for the specified interfaces
/// will be automatically generated for consumption on C# side.
/// </summary>
/// <remarks>
/// Generated bindings have to be implemented on JavaScript side.
/// For example, given "IFrontend" interface is imported, "JSFrontend" class will be generated,
/// which has to be implemented in JavaScript.<br/>
/// When an interface method starts with "Notify", an event bindings will ge generated (instead of function);
/// JavaScript name of the event will start with "on" instead of "Notify".
/// This behaviour can be configured via preferences.
/// </remarks>
/// <example>
/// Generate JavaScript APIs based on "IFrontendAPI" and "IOtherFrontendAPI" interfaces:
/// <code>
/// [assembly: JSImport(
///     typeof(IFrontendAPI),
///     typeof(IOtherFrontendAPI)
/// )]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class JSImportAttribute : JSTypeAttribute
{
    /// <inheritdoc/>
    public JSImportAttribute (params Type[] types)
        : base(types) { }
}
