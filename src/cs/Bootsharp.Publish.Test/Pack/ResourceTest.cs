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
    public void WhenDebugEnabledDebugArtifactsIncluded ()
    {
        Task.Debug = true;
        AddAssembly("Foo.dll");
        Project.WriteFile("Foo.pdb", "MockPdbContent");
        Project.WriteFile("dotnet.native.js.symbols", "MockSymbolsContent");
        Execute();
        Contains("""{ name: "Foo.pdb" }""");
        Contains("""{ name: "dotnet.native.js.symbols" }""");
    }

    [Fact]
    public void WhenDebugDisabledDebugArtifactsNotIncluded ()
    {
        Task.Debug = false;
        AddAssembly("Foo.dll");
        Project.WriteFile("Foo.pdb", "MockPdbContent");
        Project.WriteFile("dotnet.native.js.symbols", "MockSymbolsContent");
        Execute();
        DoesNotContain("""{ name: "Foo.pdb" }""");
        DoesNotContain("""{ name: "dotnet.native.js.symbols" }""");
    }

    [Fact]
    public void WhenGlobalizationEnabledIcuIncluded ()
    {
        Task.Globalization = true;
        AddAssembly("Foo.dll");
        Project.WriteFile("icudt.dat", "MockIcuContent");
        Execute();
        Contains("""{ name: "icudt.dat" }""");
    }

    [Fact]
    public void WhenGlobalizationDisabledIcuNotIncluded ()
    {
        Task.Globalization = false;
        AddAssembly("Foo.dll");
        Project.WriteFile("icudt.dat", "MockIcuContent");
        Execute();
        DoesNotContain("""{ name: "icudt.dat" }""");
    }
}
