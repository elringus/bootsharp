using System;
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
}
