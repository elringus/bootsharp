namespace Bootsharp;

/// <summary>
/// Manages exported (C# -> JavaScript) instanced interop interfaces.
/// </summary>
public static class Instances
{
    private static readonly Dictionary<int, object> idToInstance = [];
    private static readonly Queue<int> idPool = [];
    private static int maxId = int.MinValue;

    public static int Register (object instance)
    {
        var id = idPool.Count > 0 ? idPool.Dequeue() : ++maxId;
        idToInstance[id] = instance;
        return id;
    }

    public static object Get (int id) => idToInstance[id];

    /// <summary>
    /// Notifies that C# -> JS (exported) interop instance is no longer used on
    /// JS side (eg, was garbage collected) and can be released on C# side as well.
    /// </summary>
    /// <param name="id">ID of the disposed interop instance.</param>
    public static void Dispose (int id)
    {
        idToInstance.Remove(id);
        idPool.Enqueue(id);
    }
}
