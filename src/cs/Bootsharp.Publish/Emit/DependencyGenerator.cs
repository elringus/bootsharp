using System.Diagnostics.CodeAnalysis;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace Bootsharp.Publish;

/// <summary>
/// Generates hints for DotNet to not trim specified dynamic dependencies, ie
/// members that are not explicitly accessed in the user source code.
/// </summary>
internal sealed class DependencyGenerator (string entryAssembly)
{
    private readonly HashSet<string> added = [];

    public string Generate (AssemblyInspection inspection)
    {
        AddGeneratedCommon();
        AddGeneratedExportImport(inspection);
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
        Add(All, "Bootsharp.Generated.SerializerContext", entryAssembly);
        Add(All, "Bootsharp.Generated.Interop", entryAssembly);
    }

    private void AddGeneratedExportImport (AssemblyInspection inspection)
    {
        foreach (var export in inspection.Exports)
            Add(All, $"Bootsharp.Generated.Exports.{export.FullName}", entryAssembly);
        foreach (var import in inspection.Imports)
            Add(All, $"Bootsharp.Generated.Imports.{import.FullName}", entryAssembly);
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
