using System;
using Bootsharp;
using Bootsharp.Inject;
using Microsoft.Extensions.DependencyInjection;
using Test.Types;

[assembly: Export(typeof(IExportedStatic))]
[assembly: Import(typeof(IImportedStatic), typeof(IRegistryProvider))]

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

    [Import]
    public static partial void OnMainInvoked ();
}
