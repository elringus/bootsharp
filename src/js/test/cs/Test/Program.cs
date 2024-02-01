using System;
using System.Threading.Tasks;
using Bootsharp;
using Bootsharp.Inject;
using Microsoft.Extensions.DependencyInjection;
using Test.Types;

[assembly: JSExport([typeof(IExportedStatic)])]
[assembly: JSImport([typeof(IImportedStatic)])]

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
        OnMainInvoked();
    }

    [JSFunction]
    public static partial void OnMainInvoked ();

    [JSInvokable]
    public static async Task<string> GetExportedArgAndVehicleIdAsync (Vehicle vehicle, string arg)
    {
        var exported = services.GetService<IExportedStatic>()!;
        var instance = exported.GetInstance(arg);
        return await instance.GetVehicleIdAsync(vehicle) + instance.GetInstanceArg();
    }

    [JSInvokable]
    public static async Task<string> GetImportedArgAndVehicleIdAsync (Vehicle vehicle, string arg)
    {
        var imported = services.GetService<IImportedStatic>()!;
        var instance = imported.GetInstance(arg);
        return await instance.GetVehicleIdAsync(vehicle) + instance.GetInstanceArg();
    }
}
