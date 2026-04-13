using System;
using System.Threading.Tasks;
using Bootsharp;
using Bootsharp.Inject;
using Microsoft.Extensions.DependencyInjection;
using Test.Types;

[assembly: JSExport(typeof(IExportedStatic))]
[assembly: JSImport(typeof(IImportedStatic), typeof(IRegistryProvider))]

namespace Test;

public static partial class Program
{
    private static IServiceProvider services = null!;

    public static void Main ()
    {
        services = new ServiceCollection()
            .AddSingleton<IExportedStatic, ExportedStatic>()
            .AddBootsharp()
            .BuildServiceProvider()
            .RunBootsharp();
        Registry.Provider = services.GetRequiredService<IRegistryProvider>();
        OnMainInvoked();
    }

    [JSFunction]
    public static partial void OnMainInvoked ();

    [JSInvokable]
    public static async Task<string> GetExportedArgAndVehicleIdAsync (Vehicle vehicle, string arg)
    {
        var exported = services.GetService<IExportedStatic>()!;
        var instance = await exported.GetInstanceAsync(arg);
        return await instance.GetVehicleIdAsync(vehicle) + instance.GetInstanceArg();
    }

    [JSInvokable]
    public static async Task<string> GetImportedArgAndVehicleIdAsync (Vehicle vehicle, string arg)
    {
        var imported = services.GetService<IImportedStatic>()!;
        var instance = await imported.GetInstanceAsync(arg);
        return await instance.GetVehicleIdAsync(vehicle) + instance.GetInstanceArg();
    }

    [JSInvokable]
    public static async Task<string[]> GetImportedArgsAndFinalize (string arg1, string arg2)
    {
        var imported = services.GetService<IImportedStatic>()!;
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
}
