using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Bootsharp;

namespace Test;

public static partial class BCL
{
    private static CancellationTokenSource cts = new();

    [Export] public static CancellationToken ExportCancellationToken () => (cts = new()).Token;
    [Export] public static void CancelExportedCancellationToken () => cts.Cancel();
    [Import] public static partial CancellationToken ImportCancellationToken ();
    [Import] public static partial void CancelImportedCancellationToken ();
    [Export] public static CancellationToken EchoCancellationTokenExport (CancellationToken ct) => ct;
    [Import] public static partial CancellationToken EchoCancellationTokenImport (CancellationToken ct);

    [Export]
    public static void TestCancellationTokenImport ()
    {
        var cancelled = false;
        var ct = ImportCancellationToken();
        ct.Register(() => cancelled = true);
        Assert(ct.CanBeCanceled);
        Assert(!ct.IsCancellationRequested);
        CancelImportedCancellationToken();
        Assert(ct.IsCancellationRequested);
        Assert(cancelled);
        CancelImportedCancellationToken();
        CancelImportedCancellationToken();
        Assert(ct.IsCancellationRequested);
        var source = new CancellationTokenSource();
        var echoed = EchoSource(source);
        Assert(ReferenceEquals(source, echoed));
        Assert(ReferenceEquals(source, EchoSource(echoed)));
        Assert(!echoed.IsCancellationRequested);
        source.Cancel();
        Assert(echoed.IsCancellationRequested);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_source")]
        static extern ref CancellationTokenSource GetSource (ref CancellationToken ct);
        static CancellationTokenSource EchoSource (CancellationTokenSource src)
        {
            var ct = EchoCancellationTokenImport(src.Token);
            return GetSource(ref ct);
        }
    }

    [Export] public static ICollection<string> ExportCollection (string[] items) => new List<string>(items);
    [Import] public static partial ICollection<string> ImportCollection (string[] items);
    [Export] public static ICollection<string> EchoCollectionExport (ICollection<string> cl) => cl;
    [Import] public static partial ICollection<string> EchoCollectionImport (ICollection<string> cl);

    [Export]
    public static void TestCollectionImport ()
    {
        var cl = ImportCollection(["a", "b"]);
        Assert(cl.Count == 2);
        Assert(cl.SequenceEqual(["a", "b"]));
        Assert(cl.Contains("a"));
        Assert(!cl.Contains("z"));
        cl.Add("c");
        var concat = "";
        foreach (var item in cl) concat += item;
        Assert(concat == "abc");
        Assert(cl.Remove("a"));
        Assert(!cl.Remove("z"));
        Assert(cl.SequenceEqual(["b", "c"]));
        cl.Clear();
        Assert(cl.Count == 0);
        cl.Add("d");
        Assert(cl.SequenceEqual(["d"]));
        var source = new List<string> { "foo", "bar" };
        var echoed = EchoCollectionImport(source);
        Assert(ReferenceEquals(source, echoed));
        Assert(ReferenceEquals(source, EchoCollectionImport(echoed)));
        source.Clear();
        Assert(echoed.Count == 0);
    }

    [Export] public static IList<string> ExportList (string[] items) => new List<string>(items);
    [Import] public static partial IList<string> ImportList (string[] items);
    [Export] public static IList<string> EchoListExport (IList<string> list) => list;
    [Import] public static partial IList<string> EchoListImport (IList<string> list);

    [Export]
    public static void TestListImport ()
    {
        var list = ImportList(["a", "b"]);
        Assert(list.Count == 2);
        Assert(list[0] == "a");
        Assert(list[1] == "b");
        list[0] = "x";
        Assert(list[0] == "x");
        list[0] = "a";
        Assert(list.IndexOf("a") == 0);
        Assert(list.IndexOf("b") == 1);
        Assert(list.SequenceEqual(["a", "b"]));
        Assert(list.Contains("a"));
        Assert(!list.Contains("z"));
        list.Add("c");
        var concat = "";
        foreach (var item in list) concat += item;
        Assert(concat == "abc");
        Assert(list.Remove("a"));
        Assert(!list.Remove("z"));
        list.Insert(1, "z");
        Assert(list.SequenceEqual(["b", "z", "c"]));
        list.RemoveAt(1);
        Assert(list.SequenceEqual(["b", "c"]));
        list.Clear();
        Assert(list.Count == 0);
        list.Add("d");
        Assert(list.SequenceEqual(["d"]));
        var source = new List<string> { "foo", "bar" };
        var echoed = EchoListImport(source);
        Assert(ReferenceEquals(source, echoed));
        Assert(ReferenceEquals(source, EchoListImport(echoed)));
        source.Clear();
        Assert(echoed.Count == 0);
    }

    [Export] public static IDictionary<string, string> ExportDictionary (Dictionary<string, string> kv) => kv.ToDictionary();
    [Import] public static partial IDictionary<string, string> ImportDictionary (Dictionary<string, string> kv);
    [Export] public static IDictionary<string, string> EchoDictionaryExport (IDictionary<string, string> dic) => dic;
    [Import] public static partial IDictionary<string, string> EchoDictionaryImport (IDictionary<string, string> dic);

    [Export]
    public static void TestDictionaryImport ()
    {
        var dic = ImportDictionary(new Dictionary<string, string> { ["a"] = "A", ["b"] = "B" });
        Assert(dic.Count == 2);
        Assert(dic["a"] == "A");
        Assert(dic["b"] == "B");
        Assert(dic.ContainsKey("a"));
        Assert(!dic.ContainsKey("z"));
        Assert(dic.TryGetValue("a", out var value) && value == "A");
        Assert(!dic.TryGetValue("z", out _));
        Assert(dic.Keys.SequenceEqual(["a", "b"]));
        Assert(dic.Values.SequenceEqual(["A", "B"]));
        dic.Add("c", "C");
        dic["c"] = "CC";
        var concat = "";
        foreach (var kv in dic) concat += $"{kv.Key}{kv.Value}";
        Assert(concat == "aAbBcCC");
        Assert(dic.Remove("a"));
        Assert(!dic.Remove("z"));
        Assert(!dic.ContainsKey("a"));
        dic.Clear();
        Assert(dic.Count == 0);
        dic.Add("d", "D");
        Assert(dic.Values.SequenceEqual(["D"]));
        var source = new Dictionary<string, string> { ["foo"] = "1", ["bar"] = "2" };
        var echoed = EchoDictionaryImport(source);
        Assert(ReferenceEquals(source, echoed));
        Assert(ReferenceEquals(source, EchoDictionaryImport(echoed)));
        source.Clear();
        Assert(echoed.Count == 0);
    }

    [Export] public static IComparer<string> ExportComparer () => Comparer<string>.Create(string.CompareOrdinal);
    [Import] public static partial IComparer<string> ImportComparer ();
    [Export] public static IComparer<string> EchoComparerExport (IComparer<string> cmp) => cmp;
    [Import] public static partial IComparer<string> EchoComparerImport (IComparer<string> cmp);

    [Export]
    public static void TestComparerImport ()
    {
        var cmp = ImportComparer();
        Assert(cmp.Compare("a", "b") < 0);
        Assert(cmp.Compare("b", "a") > 0);
        Assert(cmp.Compare("a", "a") == 0);
        Assert(cmp.Compare(null, null) == 0);
        Assert(new[] { "c", "a", "b" }.OrderBy(i => i, cmp).SequenceEqual(["a", "b", "c"]));
        var source = Comparer<string>.Create(string.CompareOrdinal);
        var echoed = EchoComparerImport(source);
        Assert(ReferenceEquals(source, echoed));
        Assert(ReferenceEquals(source, EchoComparerImport(echoed)));
    }
}
