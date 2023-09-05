namespace Bootsharp.Builder.Test;

public class ResourceTest : ContentTest
{
    protected override string TestedContent => GeneratedResources;

    [Fact]
    public void EntryAssemblyNameIsWritten ()
    {
        AddAssembly("Foo.dll");
        Task.Execute();
        Contains("entryAssemblyName: \"Foo.dll\"");
    }

    [Fact]
    public void BinariesEmbeddedWhenEnabled ()
    {
        AddAssembly("Foo.dll");
        Task.EmbedBinaries = true;
        Task.Execute();
        Contains($$"""wasm: { name: "dotnet.native.wasm", content: "{{Convert.ToBase64String(Project.MockWasmBinary)}}" },""");
        Contains("{ name: \"Foo.wasm\", content: \"");
        Contains("{ name: \"Bootsharp.wasm\", content: \"");
        Contains("{ name: \"System.Runtime.wasm\", content: \"");
        Contains("{ name: \"System.Private.CoreLib.wasm\", content: \"");
    }

    [Fact]
    public void BinariesNotEmbeddedWhenDisabled ()
    {
        AddAssembly("Foo.dll");
        Task.EmbedBinaries = false;
        Task.Execute();
        Contains("""wasm: { name: "dotnet.native.wasm", content: undefined },""");
        Contains("""{ name: "Foo.wasm", content: undefined""");
        Contains("""{ name: "Bootsharp.wasm", content: undefined""");
        Contains("""{ name: "System.Runtime.wasm", content: undefined""");
        Contains("""{ name: "System.Private.CoreLib.wasm", content: undefined""");
    }
}
