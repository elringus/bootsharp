using System.Diagnostics.CodeAnalysis;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace Bootsharp.Publish.Test;

public class DependenciesTest : EmitTest
{
    protected override string TestedContent => GeneratedDependencies;

    [Fact]
    public void AddsCommonDependencies ()
    {
        Execute();
        Contains(
            """
            using System.Diagnostics.CodeAnalysis;

            namespace Bootsharp.Generated;

            public static class Dependencies
            {
                [System.Runtime.CompilerServices.ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Bootsharp.Generated.Dependencies", "System.Runtime")]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Bootsharp.Generated.Interop", "System.Runtime")]
                internal static void RegisterDynamicDependencies () { }
            }
            """);
    }

    [Fact]
    public void AddsStaticInteropInterfaceImplementations ()
    {
        AddAssembly(
            With("[assembly:JSExport(typeof(IExported), typeof(Space.IExported))]"),
            With("[assembly:JSImport(typeof(IImported), typeof(Space.IImported))]"),
            With("public interface IExported {}"),
            With("public interface IImported {}"),
            With("Space", "public interface IExported {}"),
            With("Space", "public interface IImported {}"));
        Execute();
        Added(All, "Bootsharp.Generated.Exports.JSExported");
        Added(All, "Bootsharp.Generated.Exports.Space.JSExported");
        Added(All, "Bootsharp.Generated.Imports.JSImported");
        Added(All, "Bootsharp.Generated.Imports.Space.JSImported");
    }

    [Fact]
    public void AddsInstancedInteropInterfaceImplementations ()
    {
        AddAssembly(With(
            """
            [assembly:JSExport(typeof(IExportedStatic))]
            [assembly:JSImport(typeof(IImportedStatic))]

            public interface IExportedStatic { IExportedInstancedA CreateExported (); }
            public interface IImportedStatic { IImportedInstancedA CreateImported (); }

            public interface IExportedInstancedA { }
            public interface IExportedInstancedB { }
            public interface IImportedInstancedA { }
            public interface IImportedInstancedB { }

            public class Class
            {
                 [JSInvokable] public static IExportedInstancedB CreateExported () => default;
                 [JSFunction] public static IImportedInstancedB CreateImported () => default;
            }
            """));
        Execute();
        Added(All, "Bootsharp.Generated.Exports.JSExportedStatic");
        Added(All, "Bootsharp.Generated.Imports.JSImportedStatic");
        Added(All, "Bootsharp.Generated.Imports.JSImportedInstancedA");
        Added(All, "Bootsharp.Generated.Imports.JSImportedInstancedB");
        // Export interop instances are not generated in C#; they're authored by user.
        Assert.DoesNotContain("Bootsharp.Generated.Exports.JSExportedInstanced", TestedContent);
    }

    [Fact]
    public void AddsClassesWithStaticInteropMethods ()
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
