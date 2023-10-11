using Backend;
using Backend.Prime;
using Bootsharp;
using Bootsharp.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

// Application entry point for browser-wasm build target.
// Notice, how neither domain, nor other C# backend assemblies
// are coupled with the JavaScript interop specifics
// and can be shared with other build targets (console, MAUI, etc).

// Generate C# -> JavaScript interop handlers for specified contracts.
[assembly: JSExport(typeof(IComputer))]
// Generate JavaScript -> C# interop handlers for specified contracts.
[assembly: JSImport(typeof(IPrimeUI))]
// Group all generated JavaScript artifacts under 'Computer' namespace.
[assembly: JSNamespace("^.*$", "Computer")]

// Perform dependency injection.
new ServiceCollection()
    .AddSingleton<IComputer, Prime>() // use prime computer
    .AddBootsharp() // inject generated interop handlers
    .BuildServiceProvider()
    .RunBootsharp(); // initialize interop services
