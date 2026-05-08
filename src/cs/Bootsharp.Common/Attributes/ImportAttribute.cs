namespace Bootsharp;

/// <summary>
/// When applied to a static partial method binds it with a JavaScript function.
/// When applied to a static partial property binds it with a JavaScript getter/setter.
/// When applied to a static event allows JavaScript consumers broadcast it.
/// When applied to WASM entry point assembly, JavaScript bindings for the specified module
/// interfaces will be automatically generated for consumption on the C# side.
/// </summary>
/// <remarks>
/// When used on the assembly level, generated bindings have to be implemented on the JavaScript side.
/// For example, given "IFrontend" interface is imported, "JSFrontend" class will be generated,
/// which has to be implemented in JavaScript.
/// </remarks>
/// <example>
/// Bind "GetHostName" method with a JavaScript function:
/// <code>
/// [Import]
/// public static partial string GetHostName ();
/// </code>
/// Bind "Counter" property with a JavaScript variable:
/// <code>
/// [Import]
/// public static partial int Counter { get; set; }
/// </code>
/// Allows broadcasting "OnSomething" event on the JavaScript side:
/// <code>
/// [Import]
/// public static event Action OnSomething;
/// </code>
/// Generate JavaScript APIs based on "IFrontend" and "IChromium" interfaces:
/// <code>
/// [assembly: Import(
///     typeof(IFrontend),
///     typeof(IChromium)
/// )]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Method | AttributeTargets.Event | AttributeTargets.Property)]
public sealed class ImportAttribute : Attribute
{
    /// <summary>
    /// When applied to assembly, lists the module interface types to generate import bindings for.
    /// </summary>
    public Type[] Types { get; }

    /// <param name="types">The module interface types to generate import bindings for (when applied to assembly).</param>
    public ImportAttribute (params Type[] types) => Types = types;
}
