using System;
using System.Threading.Tasks;
using Bootsharp;

namespace Test.Types;

public static class Interfaces
{
    [JSInvokable]
    public static async Task<string> GetImportedArgAndRecordIdAsync (Record record, string arg)
    {
        var instance = await GetImportedStatic().GetInstanceAsync(arg);
        return await instance.GetRecordIdAsync(record) + instance.GetInstanceArg();
    }

    [JSInvokable]
    public static string GetImportedStaticRecordIdAndSet (Record record)
    {
        var imported = GetImportedStatic();
        var currentRecordId = imported.Record?.Id ?? "";
        imported.Record = record;
        return currentRecordId;
    }

    [JSInvokable]
    public static async Task<string> GetImportedInstanceArgAndRecordIdAsync (Record record, string arg)
    {
        var instance = await GetImportedStatic().GetInstanceAsync(arg);
        var currentRecordId = instance.Record?.Id ?? "";
        instance.Record = record;
        return instance.GetInstanceArg() + currentRecordId + instance.Record.Id;
    }

    [JSInvokable]
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

    private static IImportedStatic GetImportedStatic ()
    {
        return (IImportedStatic)Bootsharp.Interfaces.Imports[typeof(IImportedStatic)].Instance;
    }
}
