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
        Added(All, "Bootsharp.Generated.Exports.JSFoo");
        Added(All, "Bootsharp.Generated.Exports.Space.JSBar");
    }

    [Fact]
    public void AddsGeneratedImportTypes ()
    {
        AddAssembly(
            With("[assembly:JSImport(typeof(IFoo), typeof(Space.IBar))]"),
            With("public interface IFoo {}"),
            With("Space", "public interface IBar {}"));
        Execute();
        Added(All, "Bootsharp.Generated.Imports.JSFoo");
        Added(All, "Bootsharp.Generated.Imports.Space.JSBar");
    }

    [Fact]
    public void AddsClassesWithInteropMethods ()
    {
        AddAssembly("Assembly.With.Dots.dll",
            With("SpaceA", "public class ClassA { [JSInvokable] public static void Foo () {} }"),
            With("SpaceB.SpaceC", "public class ClassB { [JSFunction] public static void Foo () {} }"),
            With("public class ClassC { [JSEvent] public static void Foo () {} }"));
        Execute();
        Added(All, "SpaceA.ClassA", "Assembly.With.Dots");
        Added(All, "SpaceB.SpaceC.ClassB", "Assembly.With.Dots");
        Added(All, "ClassC", "Assembly.With.Dots");
    }

    private void Added (DynamicallyAccessedMemberTypes types, string name) =>
        Added(types, name, Path.GetFileNameWithoutExtension(Task.EntryAssemblyName));

    private void Added (DynamicallyAccessedMemberTypes types, string name, string assembly)
    {
        Contains($"""[DynamicDependency(DynamicallyAccessedMemberTypes.{types}, "{name}", "{assembly}")]""");
    }
}
