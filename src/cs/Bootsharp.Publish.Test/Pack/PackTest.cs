namespace Bootsharp.Publish.Test;

public class PackTest : TaskTest
{
    protected BootsharpPack Task { get; }
    protected byte[] MockWasmBinary { get; } = "MockWasmContent"u8.ToArray();
    protected string MockDotNetContent { get; } = "MockDotNetContent";
    protected string MockRuntimeContent { get; } = "MockRuntimeContent";
    protected string MockNativeContent { get; } = "MockNativeContent";
    protected string GeneratedBindings => ReadProjectFile("generated/bindings.g.mjs");
    protected string GeneratedDeclarations => ReadProjectFile("generated/bindings.g.d.mts");
    protected string GeneratedResources => ReadProjectFile("generated/resources.g.mjs");

    public PackTest ()
    {
        Task = CreateTask();
        Project.WriteFile("dotnet.js", MockDotNetContent);
        Project.WriteFile("dotnet.runtime.js", MockRuntimeContent);
        Project.WriteFile("dotnet.native.js", MockNativeContent);
        Project.WriteFile("dotnet.runtime.g.js", "MockRuntimeGeneratedContent");
        Project.WriteFile("dotnet.native.g.js", "MockNativeGeneratedContent");
        Project.WriteFile("dotnet.native.wasm", MockWasmBinary);
    }

    public override void Execute ()
    {
        if (LastAddedAssemblyName is not null)
            Task.EntryAssemblyName = LastAddedAssemblyName;
        Task.Execute();
    }

    protected override void AddAssembly (string assemblyName, params MockSource[] sources)
    {
        base.AddAssembly(assemblyName, sources);
        Project.WriteFile(assemblyName[..^3] + "wasm", "");
    }

    private BootsharpPack CreateTask () => new() {
        BuildDirectory = Project.Root,
        DebugDirectory = Project.Root,
        InspectedDirectory = Project.Root,
        EntryAssemblyName = "System.Runtime.dll",
        BuildEngine = Engine,
        Globalization = false,
        LLVM = false,
        Debug = false
    };
}
