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
        Assert(await imported.GetBiAsync() is not BidirectionalCS);
        Assert(await imported.GetBiAsync(() => new BidirectionalCS()) is BidirectionalCS);
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

    [Export] public static IBidirectional ExportBi () => new BidirectionalCS();
    [Import] public static partial IBidirectional ImportBi ();

    [Export]
    public static void CanInteropWithBidirectional ()
    {
        var js = ImportBi();
        var cs = new BidirectionalCS();
        IBidirectional? eventObserved = null;
        IBidirectional? specialObserved = null;
        Action<IBidirectional?> eventHandler = b => eventObserved = b;
        IBidirectional.SpecialHandler specialHandler = b => specialObserved = b;
        js.OnBiChanged += eventHandler;
        js.OnSpecial.Subscribe(specialHandler);
        Assert(js.EchoBi(null) == null);
        Assert(js.EchoBi(js) == js);
        Assert(js.EchoBi(cs) == cs);
        js.Bi = cs;
        Assert(eventObserved == cs);
        Assert(specialObserved == cs);
        Assert(js.Bi == cs);
        js.Bi = js;
        Assert(eventObserved == js);
        Assert(specialObserved == js);
        Assert(js.Bi == js);
        js.Bi = null;
        Assert(eventObserved == null);
        Assert(specialObserved == null);
        Assert(js.Bi == null);
        js.OnBiChanged -= eventHandler;
        js.OnSpecial.Unsubscribe(specialHandler);
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
