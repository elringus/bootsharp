namespace Bootsharp;

/// <summary>
/// Base class for generated proxies used to bind JS-originated instances.
/// </summary>
public abstract class JSProxy (int id)
{
    /// <summary>
    /// Unique identifier of the proxied JS instance.
    /// </summary>
    protected internal readonly int _id = id;
}
