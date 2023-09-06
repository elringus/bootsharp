using Backend;
using Backend.Prime;
using Bootsharp;
using Microsoft.Extensions.DependencyInjection;

// Application entry point for browser-wasm build target.
// Notice, how neither domain, nor other C# backend assemblies
// are coupled with the JavaScript interop specifics
// and can be shared with other build targets (console, MAUI, etc).

// Auto-generate JavaScript interop handlers for specified contracts.
[assembly: JSExport(typeof(IComputer))]
[assembly: JSImport(typeof(IComputerUI))]

// Perform dependency injection.
new ServiceCollection()
    .AddSingleton<IComputer, PrimeComputer>() // use prime computer
    .AddBootsharp() // auto-injects generated interop handlers
    .BuildServiceProvider()
    .BuildBootsharp(); // auto-instantiates interop services
