using System;
using static Packer.TextUtilities;

namespace Packer;

internal class LibraryGenerator
{
    private readonly AssemblyInspector inspector;
    private readonly BindingsGenerator bindingsGenerator;
    private readonly ProxiesGenerator proxiesGenerator;
    private readonly string runtimeJS;
    private readonly string entryAssemblyName;
    private readonly bool worker;

    public LibraryGenerator (NamespaceBuilder spaceBuilder, AssemblyInspector inspector,
        string runtimeJS, string entryAssemblyName, bool worker)
    {
        this.inspector = inspector;
        this.runtimeJS = runtimeJS;
        this.entryAssemblyName = entryAssemblyName;
        this.worker = worker;
        bindingsGenerator = new(inspector, spaceBuilder);
        proxiesGenerator = new(inspector, spaceBuilder);
    }

    public string GenerateSideLoad (string wasmUri)
    {
        var bootUrisJS = new BootUrisTemplate {
            WasmUri = wasmUri,
            Assemblies = inspector.Assemblies,
            EntryAssemblyUri = entryAssemblyName
        }.Build();
        return worker ? GenerateLibrary(new WorkerTemplate {
            LibraryJS = GenerateLibrary(bootUrisJS),
            ProxiesJS = proxiesGenerator.Generate()
        }.Build() + bootUrisJS) : GenerateLibrary(bootUrisJS);
    }

    public string GenerateEmbedded (byte[] wasmBytes)
    {
        var embedJS = new EmbedTemplate {
            RuntimeWasm = Convert.ToBase64String(wasmBytes),
            Assemblies = inspector.Assemblies,
            EntryAssemblyName = entryAssemblyName
        }.Build();
        return worker ? GenerateLibrary(new WorkerTemplate {
            LibraryJS = GenerateLibrary(embedJS),
            ProxiesJS = proxiesGenerator.Generate()
        }.Build()) : GenerateLibrary(embedJS);
    }

    private string GenerateLibrary (string initJS) => new LibraryTemplate {
        RuntimeJS = runtimeJS,
        InitJS = JoinLines(bindingsGenerator.Generate(), initJS)
    }.Build();
}
