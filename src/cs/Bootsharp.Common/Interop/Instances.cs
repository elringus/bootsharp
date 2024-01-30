namespace Bootsharp;

/// <summary>
/// Manages exported (C# -> JavaScript) instanced interop interfaces.
/// </summary>
public static class Instances
{
    private static readonly Dictionary<object, int> instanceToId = [];
    private static readonly Dictionary<int, object> idToInstance = [];
    private static readonly Queue<int> idPool = [];
    private static int nextId = int.MinValue;

    /// <summary>
    /// Resolves unique ID of the specified interop instance.
    /// </summary>
    /// <param name="instance">The instance to get ID for.</param>
    public static int GetId (object instance)
    {
        if (instanceToId.TryGetValue(instance, out var id)) return id;
        id = idPool.Count > 0 ? idPool.Dequeue() : nextId++;
        idToInstance[id] = instance;
        return instanceToId[instance] = id;
    }

    /// <summary>
    /// Resolves registered instance by the specified ID.
    /// </summary>
    /// <param name="id">Unique ID of the instance to resolve.</param>
    public static object GetInstance (int id) => idToInstance[id];

    /// <summary>
    /// Notifies that interop instance is no longer used on JavaScript side
    /// (eg, was garbage collected) and can be released on C# side as well.
    /// </summary>
    /// <param name="id">ID of the disposed interop instance.</param>
    public static void Dispose (int id)
    {
        instanceToId.Remove(idToInstance[id]);
        idToInstance.Remove(id);
        idPool.Enqueue(id);
    }
}
