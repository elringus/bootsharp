using System.Runtime.CompilerServices;

namespace Bootsharp;

/// <summary>
/// Tracks pending <see cref="TaskCompletionSource{T}"/> instances awaiting completion from JavaScript.
/// </summary>
/// <remarks>
/// Used by Bootsharp's generated async bridge for C# → JS imports that return <see cref="Task"/> /
/// <see cref="Task{T}"/>. Storage is an array-backed slot pool with a free-list of recycled IDs;
/// WASM is single-threaded so no locking is required.
/// </remarks>
public static class PendingImports
{
    private static object?[] slots = new object?[64];
    private static int[] freeList = new int[64];
    private static int freeCount;
    private static int next;

    /// <summary>
    /// Reserves a slot for the specified completion source and returns its identifier.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Allocate (object tcs)
    {
        int id;
        if (freeCount > 0) id = freeList[--freeCount];
        else
        {
            if (next == slots.Length) Array.Resize(ref slots, slots.Length * 2);
            id = next++;
        }
        slots[id] = tcs;
        return id;
    }

    /// <summary>
    /// Removes and returns the completion source associated with the specified identifier.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskCompletionSource<T> Take<T> (int id)
    {
        var tcs = Unsafe.As<TaskCompletionSource<T>>(slots[id]!);
        slots[id] = null;
        if (freeCount == freeList.Length) Array.Resize(ref freeList, freeList.Length * 2);
        freeList[freeCount++] = id;
        return tcs;
    }
}
