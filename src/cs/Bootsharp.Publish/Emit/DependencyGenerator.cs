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
                  {{Fmt(added)}}
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
        foreach (var it in inspection.StaticInterfaces)
            Add(All, it.FullName, entryAssembly);
        foreach (var it in inspection.InstancedInterfaces)
            if (it.Interop == InteropKind.Import)
                Add(All, it.FullName, entryAssembly);
    }

    private void AddClassesWithInteropMethods (SolutionInspection inspection)
    {
        foreach (var member in inspection.StaticMembers)
            Add(All, member.Space, member.Assembly);
    }

    private void Add (DynamicallyAccessedMemberTypes types, string name, string assembly)
    {
        var asm = assembly.EndsWith(".dll", StringComparison.Ordinal) ? assembly[..^4] : assembly;
        added.Add($"""[DynamicDependency(DynamicallyAccessedMemberTypes.{types}, "{name}", "{asm}")]""");
    }
}
