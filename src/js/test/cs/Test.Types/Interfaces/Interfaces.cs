using System;
using System.Threading.Tasks;
using Bootsharp;

namespace Test.Types;

public static class Interfaces
{
    [Export] public static event Action<Record?>? OnImportedModuleRecordEchoed;
    [Export] public static event Action<string?>? OnImportedInstanceRecordEchoed;

    [Export]
    public static async Task<string> GetImportedArgAndRecordIdAsync (Record record, string arg)
    {
        var instance = await GetImportedModule().GetInstanceAsync(arg);
        return await instance.GetRecordIdAsync(record) + instance.GetInstanceArg();
    }

    [Export]
    public static string GetImportedModuleRecordIdAndSet (Record record)
    {
        var imported = GetImportedModule();
        var currentRecordId = imported.Record?.Id ?? "";
        imported.Record = record;
        return currentRecordId;
    }

    [Export]
    public static async Task<string> GetImportedInstanceArgAndRecordIdAsync (Record record, string arg)
    {
        var instance = await GetImportedModule().GetInstanceAsync(arg);
        var currentRecordId = instance.Record?.Id ?? "";
        instance.Record = record;
        return instance.GetInstanceArg() + currentRecordId + instance.Record.Id;
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

    [Export]
    public static Task EchoImportedModuleRecordEventAsync ()
    {
        var imported = GetImportedModule();
        var tcs = new TaskCompletionSource();
        imported.OnRecordChanged += Handle;
        return tcs.Task;

        void Handle (Record? record)
        {
            imported.OnRecordChanged -= Handle;
            OnImportedModuleRecordEchoed?.Invoke(record);
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
            OnImportedInstanceRecordEchoed?.Invoke(record?.Id);
            tcs.SetResult();
        }
    }

    [Export]
    public static string? CanInteropWithImportedInnerInstances (IImportedInstanced imported)
    {
        var inner = imported.Inner;
        var currentCount = -1;
        inner.OnCountChanged += HandleCountChanged;
        inner.Count = 0;
        if (currentCount != 0) return $"Set test failed. Expected count '0', but was '{currentCount}'.";
        inner.Increment();
        if (currentCount != 1) return $"Increment test failed. Expected count '1', but was '{currentCount}'.";
        inner.Increment();
        if (inner.Count != 2) return $"Get test failed. Expected count '2', but was '{currentCount}'.";
        inner.OnCountChanged -= HandleCountChanged;
        return null;

        void HandleCountChanged (int count) => currentCount = count;
    }

    private static IImportedModule GetImportedModule ()
    {
        return (IImportedModule)Modules.Imports[typeof(IImportedModule)].Instance;
    }
}
