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
}
