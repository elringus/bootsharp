// Specifying JavaScript APIs to generate bindings for.
[assembly: Bootsharp.JSImport(typeof(IFrontend))]
// Specifying C# APIs to generate bindings for.
[assembly: Bootsharp.JSExport(typeof(IBackend))]

// Using generated C# bindings to inject implementation (usually handled by DI).
_ = new Backend.JSBackend(new SharpBackend());
// Using generated JavaScript bindings to invoke 'Frontend.getName()' function.
System.Console.WriteLine($"Hello {Frontend.JSFrontend.GetName()}, Sharp here!");

// Improvised API of JavaScript frontend.
public interface IFrontend { string GetName (); }
// Improvised API of C# backend.
public interface IBackend { string GetName (); }
// Implementation of the backend.
class SharpBackend : IBackend { public string GetName () => "Sharp"; }
