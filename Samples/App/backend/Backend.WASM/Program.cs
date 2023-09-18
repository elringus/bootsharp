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
[assembly: JSImport(typeof(IPrimeComputerUI))]

// Perform dependency injection.
new ServiceCollection()
    .AddSingleton<IComputer, PrimeComputer>() // use prime computer
    .AddBootsharp() // auto-injects generated interop handlers
    .BuildServiceProvider()
    .RunBootsharp(); // auto-initialize interop services

// ----------------------------------------------------------------------
// MSBuild trims "Computer.JSComputer"'s methods when AOT enabled,
// unless its assembly's full name is accessed here.
// Yes, specifically full name property of the assembly.
// Not just type. Not assembly of the type. Bull full name of the type's assembly.
// Furthermore, the assembly in question is this very one, the entry assembly.
// [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Computer.JSComputer))] doesn't help.
// When AOT is disabled, everything works fine, even with aggressive trimming.
// TODO: This could be a bug in the .NET runtime or SDK; re-check in next releases.
// _ = typeof(Computer.JSComputer).Assembly.FullName;
