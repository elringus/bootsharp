using System;
using Bootsharp;
using Bootsharp.Inject;
using Microsoft.Extensions.DependencyInjection;
using Test.Types;

[assembly: Export(typeof(IExportedModule))]
[assembly: Import(typeof(IImportedModule), typeof(IRegistryProvider))]

namespace Test;

public static partial class Program
{
    private static IServiceProvider services = null!;

    public static void Main ()
    {
        services = new ServiceCollection()
            .AddSingleton<IExportedModule, ExportedModule>()
            .AddBootsharp()
            .BuildServiceProvider()
            .RunBootsharp();
        Registries.Provider = services.GetRequiredService<IRegistryProvider>();
        OnMainInvoked();
    }

    [Import]
    public static partial void OnMainInvoked ();
}
