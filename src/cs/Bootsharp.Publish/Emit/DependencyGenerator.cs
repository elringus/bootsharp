using System.Diagnostics.CodeAnalysis;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace Bootsharp.Publish;

/// <summary>
/// Generates hints for .NET to not trim specified dynamic dependencies, ie
/// members that are not explicitly accessed in the user source code.
/// </summary>
internal sealed class DependencyGenerator (string entryAssembly)
{
    private readonly HashSet<string> added = [];

    public string Generate (SolutionInspection inspection)
    {
        AddGeneratedCommon();
        AddGeneratedInteropClasses(inspection);
        AddClassesWithInteropMethods(inspection);
        return
            $$"""
              using System.Diagnostics.CodeAnalysis;

              namespace Bootsharp.Generated;

              public static class Dependencies
              {
                  [System.Runtime.CompilerServices.ModuleInitializer]
                  {{JoinLines(added)}}
                  internal static void RegisterDynamicDependencies () { }
              }
              """;
    }

    private void AddGeneratedCommon ()
    {
        Add(All, "Bootsharp.Generated.Dependencies", entryAssembly);
        Add(All, "Bootsharp.Generated.Interop", entryAssembly);
    }

    private void AddGeneratedInteropClasses (SolutionInspection inspection)
    {
        foreach (var inter in inspection.StaticInterfaces)
            Add(All, inter.FullName, entryAssembly);
        foreach (var inter in inspection.InstancedInterfaces)
            Add(All, inter.FullName, entryAssembly);
    }

    private void AddClassesWithInteropMethods (SolutionInspection inspection)
    {
        foreach (var method in inspection.StaticMethods)
            Add(All, method.Space, method.Assembly);
    }

    private void Add (DynamicallyAccessedMemberTypes types, string name, string assembly)
    {
        var asm = assembly.EndsWith(".dll", StringComparison.Ordinal) ? assembly[..^4] : assembly;
        added.Add($"""[DynamicDependency(DynamicallyAccessedMemberTypes.{types}, "{name}", "{asm}")]""");
    }
}
