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
                [System.Runtime.CompilerServices.ModuleInitializer]
                [DynamicDependency(types, "Bootsharp.Generated.Dependencies", "System.Runtime")]
                [DynamicDependency(types, "Bootsharp.Generated.Interop", "System.Runtime")]
                internal static void RegisterDynamicDependencies () { }
            """);
    }

    [Fact]
    public void AddsStaticInterfaceImplementations ()
    {
        AddAssembly(
            With("[assembly:Export(typeof(IExported), typeof(Space.IExported))]"),
            With("[assembly:Import(typeof(IImported), typeof(Space.IImported))]"),
            With("public interface IExported {}"),
            With("public interface IImported {}"),
            With("Space", "public interface IExported {}"),
            With("Space", "public interface IImported {}"));
        Execute();
        Added("Bootsharp.Generated.Exports.JSExported");
        Added("Bootsharp.Generated.Exports.Space.JSExported");
        Added("Bootsharp.Generated.Imports.JSImported");
        Added("Bootsharp.Generated.Imports.Space.JSImported");
    }

    [Fact]
    public void AddsInstancedInterfaceImplementations ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(IExportedStatic))]
            [assembly:Import(typeof(IImportedStatic))]

            public interface IExportedStatic { IExportedInstancedA CreateExported (); }
            public interface IImportedStatic { IImportedInstancedA CreateImported (); }

            public interface IExportedInstancedA { }
            public interface IExportedInstancedB { }
            public interface IImportedInstancedA { }
            public interface IImportedInstancedB { }

            public class Class
            {
                 [Export] public static IExportedInstancedB CreateExported () => default;
                 [Import] public static IImportedInstancedB CreateImported () => default;
            }
            """));
        Execute();
        Added("Bootsharp.Generated.Exports.JSExportedStatic");
        Added("Bootsharp.Generated.Imports.JSImportedStatic");
        Added("Bootsharp.Generated.Imports.JSImportedInstancedA");
        Added("Bootsharp.Generated.Imports.JSImportedInstancedB");
        // Export interop instances are not generated in C#; they're authored by user.
        DoesNotContain("Bootsharp.Generated.Exports.JSExportedInstanced");
    }

    [Fact]
    public void AddsClassesWithStaticInteropMembers ()
    {
        AddAssembly("Assembly.With.Dots.dll",
            With("SpaceA", "public class ClassA { [Export] public static void Foo () {} }"),
            With("SpaceB.SpaceC", "public class ClassB { [Import] public static void Foo () {} }"),
            With("public class ClassC { [Export] public static event Action? Evt; }"));
        Execute();
        Added("SpaceA.ClassA", "Assembly.With.Dots");
        Added("SpaceB.SpaceC.ClassB", "Assembly.With.Dots");
        Added("ClassC", "Assembly.With.Dots");
    }

    private void Added (string name) =>
        Added(name, Path.GetFileNameWithoutExtension(Task.EntryAssemblyName));

    private void Added (string name, string assembly)
    {
        Contains($"""[DynamicDependency(types, "{name}", "{assembly}")]""");
    }
}
