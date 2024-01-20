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

    public string Generate (AssemblyInspection inspection)
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

    private void AddGeneratedInteropClasses (AssemblyInspection inspection)
    {
        foreach (var inter in inspection.Interfaces)
            Add(All, inter.FullName, entryAssembly);
    }

    private void AddClassesWithInteropMethods (AssemblyInspection inspection)
    {
        foreach (var method in inspection.Methods)
            Add(All, method.Space, method.Assembly);
    }

    private void Add (DynamicallyAccessedMemberTypes types, string name, string assembly)
    {
        var asm = assembly.EndsWith(".dll", StringComparison.Ordinal) ? assembly[..^4] : assembly;
        added.Add($"""[DynamicDependency(DynamicallyAccessedMemberTypes.{types}, "{name}", "{asm}")]""");
    }
}
