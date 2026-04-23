using System;
using System.Threading.Tasks;
using Bootsharp;

namespace Test.Types;

public static class Interfaces
{
    [Export] public static event Action<Record?>? OnImportedStaticRecordEchoed;
    [Export] public static event RecordChanged<IImportedInstanced>? OnImportedInstanceRecordEchoed;

    [Export]
    public static async Task<string> GetImportedArgAndRecordIdAsync (Record record, string arg)
    {
        var instance = await GetImportedStatic().GetInstanceAsync(arg);
        return await instance.GetRecordIdAsync(record) + instance.GetInstanceArg();
    }

    [Export]
    public static string GetImportedStaticRecordIdAndSet (Record record)
    {
        var imported = GetImportedStatic();
        var currentRecordId = imported.Record?.Id ?? "";
        imported.Record = record;
        return currentRecordId;
    }

    [Export]
    public static async Task<string> GetImportedInstanceArgAndRecordIdAsync (Record record, string arg)
    {
        var instance = await GetImportedStatic().GetInstanceAsync(arg);
        var currentRecordId = instance.Record?.Id ?? "";
        instance.Record = record;
        return instance.GetInstanceArg() + currentRecordId + instance.Record.Id;
    }

    [Export]
    public static async Task<string[]> GetImportedArgsAndFinalize (string arg1, string arg2)
    {
        var imported = GetImportedStatic();
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

    [Export]
    public static Task EchoImportedStaticRecordEventAsync ()
    {
        var imported = GetImportedStatic();
        var tcs = new TaskCompletionSource();
        imported.OnRecordChanged += Handle;
        return tcs.Task;

        void Handle (Record? record)
        {
            imported.OnRecordChanged -= Handle;
            OnImportedStaticRecordEchoed?.Invoke(record);
            tcs.SetResult();
        }
    }

    [Export]
    public static Task EchoImportedInstanceRecordEventAsync (IImportedInstanced imported)
    {
        var tcs = new TaskCompletionSource();
        imported.OnRecordChanged += Handle;
        return tcs.Task;

        void Handle (IImportedInstanced caller, Record? record)
        {
            imported.OnRecordChanged -= Handle;
            OnImportedInstanceRecordEchoed?.Invoke(caller, record);
            tcs.SetResult();
        }
    }

    private static IImportedStatic GetImportedStatic ()
    {
        return (IImportedStatic)Bootsharp.Interfaces.Imports[typeof(IImportedStatic)].Instance;
    }
}
