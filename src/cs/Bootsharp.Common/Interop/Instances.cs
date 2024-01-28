namespace Bootsharp;

/// <summary>
/// Manages references to instanced exported (C# to JS) interop interfaces.
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

    public static void Dispose (int id)
    {
        idToInstance.Remove(id);
        idPool.Enqueue(id);
    }
}
