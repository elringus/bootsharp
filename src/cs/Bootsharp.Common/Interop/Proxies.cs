namespace Bootsharp;

/// <summary>
/// Provides access to generated interop methods for JavaScript functions and events.
/// </summary>
/// <remarks>
/// <b>Below is for internal reference; end users are not expected to use this API.</b><br/>
/// Partial interop methods (<see cref="JSFunctionAttribute"/> and <see cref="JSEventAttribute"/>)
/// are accessed via delegates registered by associated IDs, where ID is the full name of
/// the declaring type of the interop method joined with the method's name by a dot. Eg, given:<br/>
/// <code>
/// namespace Space;
/// public static partial class Class
/// {
///     [JSFunction] public static partial int Foo (string arg);
/// }
/// </code><br/>
/// Proxy for the "Foo" method is registered as follows (emitted at build;
/// actual code will have additional de-/serialization steps):<br/>
/// <code>
/// Proxies.Set("Space.Class.Foo", (arg) => Bootsharp.Generated.Interop.Space_Class_Foo(arg));
/// </code><br/>
/// Registered proxy is accessed as follows (emitted by source generator to
/// implement original partial "Foo" method):<br/>
/// <code>
/// public static int Foo (string arg) => <![CDATA[Proxies.Get<Func<string, int>>("Space.Class.Foo")(arg);]]>
/// </code><br/>
/// </remarks>
public static class Proxies
{
    private static readonly Dictionary<string, Delegate> map = new();

    /// <summary>
    /// Maps specified interop delegate to the specified ID.
    /// </summary>
    /// <remarks>
    /// Performed in the generated interop code at module initialization.
    /// </remarks>
    public static void Set (string id, Delegate @delegate)
    {
        map[id] = @delegate;
    }

    /// <summary>
    /// Returns interop delegate of specified ID and type.
    /// </summary>
    /// <remarks>
    /// Used in sources generated for partial <see cref="JSFunctionAttribute"/>
    /// and <see cref="JSEventAttribute"/> methods.
    /// </remarks>
    public static T Get<T> (string id) where T : Delegate
    {
        if (!map.TryGetValue(id, out var @delegate))
            throw new Error($"Proxy '{id}' is not found.");
        if (@delegate is not T specific)
            throw new Error($"Proxy '{id}' is not '{typeof(T)}'.");
        return specific;
    }
}
