using System;
using System.Linq;
using System.Threading.Tasks;
using Bootsharp;

namespace Test;

/// <summary>
/// Sample class documentation.
/// </summary>
public static partial class Static
{
    public enum Enum { One = 1, Two = 2 }

    [Import] public static event Action<string?>? ImportedEvent;
    [Export] public static event Action<string?>? ExportedEvent;

    [Import] public static partial string ImportedProperty { get; set; }
    [Export] public static string ExportedProperty { get; set; } = "initial exported";

    [Import] public static partial byte[] EchoImported (byte[] bytes);
    [Export] public static byte[] EchoExported (byte[] bytes) => bytes;
    [Import] public static partial Task<byte[]> EchoImportedAsync (byte[] bytes);
    [Export] public static Task<byte[]> EchoExportedAsync (byte[] bytes) => Task.Delay(1).ContinueWith(_ => bytes);

    [Import] public static partial void ImportedFunction ();
    [Export] public static void InvokeImportedFunction () => ImportedFunction();
    [Export] public static void BroadcastExportedEvent (string? payload) => ExportedEvent?.Invoke(payload);
    [Export] public static DateTime AddDays (DateTime date, int days) => date.AddDays(days);
    [Export] public static Enum GetEnum (int idx) => (Enum)idx;

    [Export]
    public static async Task CanInteropWithImportedStaticsAsync ()
    {
        var tcs = new TaskCompletionSource<string?>();
        Action<string?> handler = v => tcs.TrySetResult(v);
        ImportedEvent += handler;
        Assert(ImportedProperty == "initial imported");
        ImportedProperty = "foo";
        Assert(ImportedProperty == "foo");
        Assert(EchoImported([42, 24]).Sum(i => i) == 66);
        Assert((await EchoImportedAsync([24, 42])).Sum(i => i) == 66);
        Assert(await tcs.Task == "event payload");
        ImportedEvent -= handler;
    }
}
