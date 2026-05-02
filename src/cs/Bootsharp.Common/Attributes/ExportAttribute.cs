namespace Bootsharp;

/// <summary>
/// When applied to a static method makes it invokable in JavaScript.
/// When applied to a static <see cref="Action"/> event allows JavaScript consumers subscribe to it.
/// When applied to WASM entry point assembly, specified module class or interfaces types will
/// be automatically exported for consumption on the JavaScript side.
/// </summary>
/// <remarks>
/// When used on the assembly level, generated bindings have to be initialized with the handler implementation.
/// For example, given "IHandler" interface is exported, "JSHandler" class will be generated,
/// which has to be instantiated with an "IHandler" implementation instance.
/// </remarks>
/// <example>
/// Expose "GetName" method to JavaScript:
/// <code>
/// [Export]
/// public static string GetName () => "Sharp";
/// </code>
/// Expose "OnSomething" event to JavaScript:
/// <code>
/// [Export]
/// public static event Action OnSomething;
/// </code>
/// Expose "IService" and "Handler" C# API surfaces to JavaScript and wrap invocations in "Utils.Try()":
/// <code>
/// [assembly: Export(
///     typeof(IService),
///     typeof(Handler),
///     invokePattern = "(.+)",
///     invokeReplacement = "Utils.Try(() => $1)"
/// )]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Method | AttributeTargets.Event)]
public sealed class ExportAttribute : Attribute
{
    /// <summary>
    /// When applied to assembly, lists the module (class or interface) types to generated export bindings for.
    /// </summary>
    public Type[] Types { get; }

    /// <param name="types">The module types to generate export bindings for (when applied to assembly).</param>
    public ExportAttribute (params Type[] types) => Types = types;
}
