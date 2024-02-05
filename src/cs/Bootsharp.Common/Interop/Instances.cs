namespace Bootsharp;

/// <summary>
/// Manages exported (C# -> JavaScript) instanced interop interfaces.
/// </summary>
public static class Instances
{
    private static readonly Dictionary<int, object> idToInstance = [];
    private static readonly Queue<int> idPool = [];
    private static int nextId = int.MinValue;

    /// <summary>
    /// Registers specified interop instance and associates it with unique ID.
    /// </summary>
    /// <param name="instance">The instance to register.</param>
    /// <returns>Unique ID associated with the registered instance.</returns>
    public static int Register (object instance)
    {
        var id = idPool.Count > 0 ? idPool.Dequeue() : nextId++;
        idToInstance[id] = instance;
        return id;
    }

    /// <summary>
    /// Resolves registered instance by the specified ID.
    /// </summary>
    /// <param name="id">Unique ID of the instance to resolve.</param>
    public static object Get (int id)
    {
        if (!idToInstance.TryGetValue(id, out var instance))
            throw new Error($"Failed to resolve exported interop instance with '{id}' ID: not registered.");
        return instance;
    }

    /// <summary>
    /// Notifies that interop instance is no longer used on JavaScript side
    /// (eg, was garbage collected) and can be released on C# side as well.
    /// </summary>
    /// <param name="id">ID of the disposed interop instance.</param>
    public static void Dispose (int id)
    {
        if (!idToInstance.Remove(id))
            throw new Error($"Failed to dispose exported interop instance with '{id}' ID: not registered.");
        idPool.Enqueue(id);
    }
}
