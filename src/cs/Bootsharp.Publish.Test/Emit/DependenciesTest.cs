using System.Diagnostics.CodeAnalysis;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace Bootsharp.Publish.Test;

public class DependenciesTest : EmitTest
{
    protected override string TestedContent => GeneratedDependencies;

    [Fact]
    public void AddsCommonGeneratedTypesByDefault ()
    {
        Execute();
        Added(All, "Bootsharp.Generated.Dependencies");
        Added(All, "Bootsharp.Generated.SerializerContext");
        Added(All, "Bootsharp.Generated.Interop");
    }

    [Fact]
    public void AddsGeneratedExportTypes ()
    {
        AddAssembly(
            With("[assembly:JSExport(typeof(IFoo), typeof(Space.IBar))]"),
            With("public interface IFoo {}"),
            With("Space", "public interface IBar {}"));
        Execute();
        Added(All, "Bootsharp.Generated.Exports.IFoo");
        Added(All, "Bootsharp.Generated.Exports.Space.IBar");
    }

    [Fact]
    public void AddsGeneratedImportTypes ()
    {
        AddAssembly(
            With("[assembly:JSImport(typeof(IFoo), typeof(Space.IBar))]"),
            With("public interface IFoo {}"),
            With("Space", "public interface IBar {}"));
        Execute();
        Added(All, "Bootsharp.Generated.Imports.IFoo");
        Added(All, "Bootsharp.Generated.Imports.Space.IBar");
    }

    private void Added (DynamicallyAccessedMemberTypes types, string name) => Added(types, name, Task.EntryAssemblyName);

    private void Added (DynamicallyAccessedMemberTypes types, string name, string assembly)
    {
        Contains($"""[DynamicDependency(DynamicallyAccessedMemberTypes.{types}, "{name}", "{assembly}")]""");
    }
}
