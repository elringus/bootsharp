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

    public interface IShape
    {
        string Name { get; set; }
    }

    public class Circle : IShape
    {
        public string Name { get; set; } = "circle";
        public double GetRadius () => 3.14;
    }

    public record Square : IShape
    {
        public string Name { get; set; } = "square";
        public double Area { get; set; }
    }

    [Import] public static event Action<string?>? ImportedEvent;
    [Export] public static event Action<string?>? ExportedEvent;

    [Import] public static partial string ImportedProperty { get; set; }
    [Export] public static string ExportedProperty { get; set; } = "initial exported";
    [Import] public static partial SpecialHandlerEvent ImportedSpecial { get; }
    [Export] public static SpecialHandlerEvent ExportedSpecial { get; } = new();

    [Import] public static partial byte[] EchoImported (byte[] bytes);
    [Export] public static byte[] EchoExported (byte[] bytes) => bytes;
    [Import] public static partial Task<byte[]> EchoImportedAsync (byte[] bytes);
    [Export] public static Task<byte[]> EchoExportedAsync (byte[] bytes) => Task.Delay(1).ContinueWith(_ => bytes);

    [Import] public static partial void ImportedFunction ();
    [Export] public static void InvokeImportedFunction () => ImportedFunction();
    [Export] public static void BroadcastExportedEvent (string? payload) => ExportedEvent?.Invoke(payload);
    [Export] public static void BroadcastExportedSpecial (string? nul, string str, int? opt) => ExportedSpecial.Broadcast(nul, str, opt);
    [Export] public static DateTime AddDays (DateTime date, int days) => date.AddDays(days);
    [Export] public static Enum GetEnum (int idx) => (Enum)idx;
    [Export] public static T MakeGeneric<T> () where T : IShape, new() => new();
    [Export] public static T EchoGeneric<T> (T shape) where T : IShape => shape;
    [Export] public static string Combine (int a) => $"int:{a}";
    [Export] public static string Combine (string a) => $"str:{a}";
    [Export] public static string Combine (int a, int b) => $"sum:{a + b}";

    [Export]
    public static async Task CanInteropWithImportedStaticsAsync ()
    {
        var eventTcs = new TaskCompletionSource<string?>();
        Action<string?> eventHandler = v => eventTcs.TrySetResult(v);
        ImportedEvent += eventHandler;
        var specialTcs = new TaskCompletionSource<(string?, string, int?)>();
        SpecialHandler specialHandler = (nul, str, opt) => specialTcs.TrySetResult((nul, str, opt));
        ImportedSpecial.Subscribe(specialHandler);
        Assert(ImportedProperty == "initial imported");
        ImportedProperty = "foo";
        Assert(ImportedProperty == "foo");
        Assert(EchoImported([42, 24]).Sum(i => i) == 66);
        Assert((await EchoImportedAsync([24, 42])).Sum(i => i) == 66);
        Assert(await eventTcs.Task == "event payload");
        Assert(await specialTcs.Task == (null, "special payload", null));
        ImportedEvent -= eventHandler;
        ImportedSpecial.Unsubscribe(specialHandler);
    }
}
