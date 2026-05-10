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
}
