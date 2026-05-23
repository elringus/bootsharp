namespace Bootsharp.Publish.Test;

public class ResourceTest : GenerateJSTest
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
    public void WhenDebugEnabledDebugArtifactsIncluded ()
    {
        Task.Debug = true;
        AddAssembly("Foo.dll");
        Project.WriteFile("Foo.pdb", "MockPdbContent");
        Project.WriteFile("dotnet.native.js.symbols", "MockSymbolsContent");
        Execute();
        Contains("Foo.pdb");
        Contains("dotnet.native.js.symbols");
    }

    [Fact]
    public void WhenDebugDisabledDebugArtifactsNotIncluded ()
    {
        Task.Debug = false;
        AddAssembly("Foo.dll");
        Project.WriteFile("Foo.pdb", "MockPdbContent");
        Project.WriteFile("dotnet.native.js.symbols", "MockSymbolsContent");
        Execute();
        DoesNotContain("Foo.pdb");
        DoesNotContain("dotnet.native.js.symbols");
    }

    [Fact]
    public void WhenGlobalizationEnabledIcuIncluded ()
    {
        Task.Globalization = true;
        AddAssembly("Foo.dll");
        Project.WriteFile("icudt.dat", "MockIcuContent");
        Execute();
        Contains("icudt.dat");
    }

    [Fact]
    public void WhenGlobalizationDisabledIcuNotIncluded ()
    {
        Task.Globalization = false;
        AddAssembly("Foo.dll");
        Project.WriteFile("icudt.dat", "MockIcuContent");
        Execute();
        DoesNotContain("icudt.dat");
    }

    [Fact]
    public void WhenEmbedEnabledBinaryContentEmbeddedAsBase64 ()
    {
        Task.Embed = true;
        Task.Globalization = true;
        Task.Debug = true;
        AddAssembly("Foo.dll");
        Project.WriteFile("Foo.wasm", "MockFooContent"u8);
        Project.WriteFile("icudt.dat", "MockIcuContent"u8);
        Project.WriteFile("Foo.pdb", "MockPdbContent"u8);
        Project.WriteFile("dotnet.native.js.symbols", "MockSymbolsContent"u8);
        Execute();
        Contains($"wasm: \"{Convert.ToBase64String(MockWasmBinary)}\"");
        Contains($"{{ name: \"Foo.wasm\", content: \"{Convert.ToBase64String("MockFooContent"u8)}\" }}");
        Contains($"{{ name: \"icudt.dat\", content: \"{Convert.ToBase64String("MockIcuContent"u8)}\" }}");
        Contains($"{{ name: \"Foo.pdb\", content: \"{Convert.ToBase64String("MockPdbContent"u8)}\" }}");
        Contains($"{{ name: \"dotnet.native.js.symbols\", content: \"{Convert.ToBase64String("MockSymbolsContent"u8)}\" }}");
    }
}
