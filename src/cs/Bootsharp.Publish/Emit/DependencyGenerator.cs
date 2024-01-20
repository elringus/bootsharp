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
        var asm = Path.GetFileNameWithoutExtension(entryAssembly);
        Add(All, "Bootsharp.Generated.Dependencies", asm);
        Add(All, "Bootsharp.Generated.Interop", asm);
    }

    private void AddGeneratedInteropClasses (AssemblyInspection inspection)
    {
        var asm = Path.GetFileNameWithoutExtension(entryAssembly);
        foreach (var inter in inspection.Interfaces)
            Add(All, inter.FullName, asm);
    }

    private void AddClassesWithInteropMethods (AssemblyInspection inspection)
    {
        foreach (var method in inspection.Methods)
            Add(All, method.Space, method.Assembly);
    }

    private void Add (DynamicallyAccessedMemberTypes types, string name, string assembly)
    {
        added.Add($"""[DynamicDependency(DynamicallyAccessedMemberTypes.{types}, "{name}", "{assembly}")]""");
    }
}
