using System;
using System.Threading.Tasks;
using Bootsharp;

namespace Test.Library;

public static partial class Modules
{
    [Export]
    public static async Task CanInteropWithImportedModuleAsync ()
    {
        var imported = GetImportedModule();
        var tcs = new TaskCompletionSource<Record?>();
        IImportedModule.RecordChanged handler = r => tcs.TrySetResult(r);
        imported.OnRecordChanged += handler;
        Assert(imported.Record?.Id == "initial");
        imported.Record = new Record("set");
        Assert(imported.Record?.Id == "set");
        imported.Record = null;
        Assert(imported.Record == null);
        var instance = await imported.GetInstanceAsync("module-arg");
        Assert(instance.GetInstanceArg() == "module-arg");
        Assert((await tcs.Task)?.Id == "event-rec");
        imported.OnRecordChanged -= handler;
    }

    [Export]
    public static async Task CanInteropWithImportedInstanceAsync (IImportedInstanced imported)
    {
        var tcs = new TaskCompletionSource<Record?>();
        RecordChanged<IImportedInstanced> handler = (_, r) => tcs.TrySetResult(r);
        imported.OnRecordChanged += handler;
        Assert(imported.GetInstanceArg() == "instance-arg");
        Assert(await imported.GetRecordIdAsync(new Record("rec-id")) == "rec-id");
        Assert(imported.Record?.Id == "initial-rec");
        imported.Record = new Record("set");
        Assert(imported.Record?.Id == "set");
        Assert((await tcs.Task)?.Id == "event-rec");
        imported.OnRecordChanged -= handler;
    }

    [Export]
    public static void CanInteropWithImportedInnerInstance (IImportedInstanced imported)
    {
        var inner = imported.Inner;
        var currentCount = -1;
        Action<int> handler = c => currentCount = c;
        inner.OnCountChanged += handler;
        inner.Count = 0;
        Assert(currentCount == 0);
        inner.Increment();
        Assert(currentCount == 1);
        inner.Increment();
        Assert(inner.Count == 2);
        inner.OnCountChanged -= handler;
    }

    [Export] public static IBidirectional ExportBi () => new Bidirectional();
    [Import] public static partial IBidirectional ImportBi ();

    [Export]
    public static void CanInteropWithBidirectional ()
    {
        var js = ImportBi();
        var cs = new Bidirectional();
        IBidirectional? observed = null;
        Action<IBidirectional> handler = b => observed = b;
        js.OnBiChanged += handler;
        Assert(js.EchoBi(js) == js);
        Assert(js.EchoBi(cs) == cs);
        js.Bi = cs;
        Assert(observed == cs);
        Assert(js.Bi == cs);
        js.Bi = js;
        Assert(observed == js);
        Assert(js.Bi == js);
        js.OnBiChanged -= handler;
    }

    [Export]
    public static async Task<string[]> GetImportedArgsAndFinalize (string arg1, string arg2)
    {
        var imported = GetImportedModule();
        var instance1 = await imported.GetInstanceAsync(arg1);
        var instance2 = await imported.GetInstanceAsync(arg2);
        var result = new[] { instance1.GetInstanceArg(), instance2.GetInstanceArg() };
        instance1 = null!;
        instance2 = null!;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        return result;
    }

    private static IImportedModule GetImportedModule ()
    {
        return (IImportedModule)Bootsharp.Modules.Imports[typeof(IImportedModule)].Instance;
    }
}
