namespace Bootsharp.Publish.Test;

public class ResourceTest : PackTest
{
    protected override string TestedContent => GeneratedResources;

    [Fact]
    public void EntryAssemblyNameIsWritten ()
    {
        AddAssembly("Foo.dll");
        Execute();
        Contains("entryAssemblyName: \"Foo.dll\"");
    }

    [Fact]
    public void BinariesEmbeddedWhenEnabled ()
    {
        AddAssembly("Foo.dll");
        Task.EmbedBinaries = true;
        Execute();
        Contains($$"""wasm: { name: "dotnet.native.wasm", content: "{{Convert.ToBase64String(MockWasmBinary)}}" },""");
        Contains("{ name: \"Foo.wasm\", content: \"");
    }

    [Fact]
    public void BinariesNotEmbeddedWhenDisabled ()
    {
        AddAssembly("Foo.dll");
        Task.EmbedBinaries = false;
        Execute();
        Contains("""wasm: { name: "dotnet.native.wasm", content: undefined },""");
        Contains("""{ name: "Foo.wasm", content: undefined""");
    }

    [Fact]
    public void WhenDebugEnabledDebugArtifactsIncluded ()
    {
        Task.Debug = true;
        AddAssembly("Foo.dll");
        Project.WriteFile("Foo.pdb", "MockPdbContent");
        Project.WriteFile("dotnet.native.js.symbols", "MockSymbolsContent");
        Execute();
        Contains("""{ name: "Foo.pdb", content: undefined }""");
        Contains("""{ name: "dotnet.native.js.symbols", content: undefined }""");
    }

    [Fact]
    public void WhenDebugDisabledDebugArtifactsNotIncluded ()
    {
        Task.Debug = false;
        AddAssembly("Foo.dll");
        Project.WriteFile("Foo.pdb", "MockPdbContent");
        Project.WriteFile("dotnet.native.js.symbols", "MockSymbolsContent");
        Execute();
        DoesNotContain("""{ name: "Foo.pdb", content: undefined }""");
        DoesNotContain("""{ name: "dotnet.native.js.symbols", content: undefined }""");
    }
}
