namespace Bootsharp.Publish.Test;

public class BuildTest : TaskTest
{
    protected BootsharpPack Task { get; }
    protected byte[] MockWasmBinary { get; } = "MockWasmContent"u8.ToArray();
    protected string MockDotNetContent { get; } = "MockDotNetContent";
    protected string MockRuntimeContent { get; } = "MockRuntimeContent";
    protected string MockNativeContent { get; } = "MockNativeContent";
    protected string GeneratedBindings => ReadProjectFile("bindings.g.js");
    protected string GeneratedDeclarations => ReadProjectFile("bindings.g.d.ts");
    protected string GeneratedResources => ReadProjectFile("resources.g.js");
    protected string GeneratedDotNetModule => ReadProjectFile("dotnet.g.js");
    protected string GeneratedRuntimeModule => ReadProjectFile("dotnet.runtime.g.js");
    protected string GeneratedNativeModule => ReadProjectFile("dotnet.native.g.js");

    public BuildTest ()
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

    private BootsharpPack CreateTask () => new() {
        BuildDirectory = Project.Root,
        InspectedDirectory = Project.Root,
        EntryAssemblyName = "System.Runtime.dll",
        BuildEngine = Engine,
        TrimmingEnabled = false,
        EmbedBinaries = false,
        Threading = false
    };
}
