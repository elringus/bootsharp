using Bootsharp;
using Bootsharp.Inject;
using Microsoft.Extensions.DependencyInjection;

// Application entry point for browser-wasm build target.
// Notice, how neither domain, nor other C# backend assemblies
// are coupled with the JavaScript interop specifics
// and can be shared with other build targets (console, MAUI, etc).

// Generate C# -> JavaScript interop handlers for specified contracts.
[assembly: JSExport(typeof(Backend.IComputer))]
// Generate JavaScript -> C# interop handlers for specified contracts.
[assembly: JSImport(typeof(Backend.Prime.IPrimeUI))]
// Customize Bootsharp behaviour at build time.
[assembly: JSConfiguration<Preferences>]

// Perform dependency injection.
new ServiceCollection()
    .AddSingleton<Backend.IComputer, Backend.Prime.Prime>() // use prime computer
    .AddBootsharp() // inject generated interop handlers
    .BuildServiceProvider()
    .RunBootsharp(); // initialize interop services
