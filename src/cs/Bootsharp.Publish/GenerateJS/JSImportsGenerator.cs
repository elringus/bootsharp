namespace Bootsharp.Publish;

internal sealed class JSImportsGenerator
{
    /// <summary>
    /// Generates the runtime binding file (ESM) that populates a flat <c>BootsharpImports</c> global,
    /// keyed by the C-ABI entry point names that match the linked js-library shims.
    /// </summary>
    public string GenerateBinding (JSModules mds, IReadOnlyCollection<JSImportBinding> bindings) =>
        $$"""
          {{Fmt(mds.List.Select(EmitImport), 0)}}
          import { instances } from "../instances.mjs";

          export function bindImports() {
              const bsi = globalThis.BootsharpImports ??= {};
              {{Fmt(bindings.Select(EmitBinding))}}
              bsi.Bootsharp_Instances_NotifyImportedDisposed = (id) => instances.disposeImported(id);
          }
          """;

    /// <summary>
    /// Generates the Emscripten js-library file. The flat <c>mergeInto(LibraryManager.library, ...)</c>
    /// block is linked into the WASM module so C# <c>[DllImport]</c> calls resolve at link time.
    /// </summary>
    public string GenerateLibrary (IReadOnlyCollection<JSImportBinding> bindings) =>
        $$"""
          mergeInto(LibraryManager.library, {
              {{Fmt(bindings.Select(EmitLibraryEntry).Concat([
                  "Bootsharp_Instances_NotifyImportedDisposed: function (id) { return BootsharpImports.Bootsharp_Instances_NotifyImportedDisposed(id); }"
              ]), separator: ",\n")}}
          });
          """;

    /// <summary>
    /// Enumerates every JS-side import binding (C# → JS calls) that must appear in both the js-library
    /// (mergeInto stubs) and the runtime binding (BootsharpImports global). Mirrors InteropGenerator's
    /// <c>[DllImport]</c> emit logic to ensure entry-point names match symbol-for-symbol.
    /// </summary>
    public IReadOnlyList<JSImportBinding> Collect (JSModules mds, IReadOnlyCollection<SurfaceMeta> surfaces)
    {
        var list = new List<JSImportBinding>();
        foreach (var srf in surfaces)
        foreach (var member in srf.Members)
            CollectMember(srf, member, mds, list);
        return list;
    }

    private static void CollectMember (SurfaceMeta srf, MemberMeta member, JSModules mds, List<JSImportBinding> list)
    {
        var path = ResolveModulePath(srf, mds);
        switch (member)
        {
            case EventMeta { IK: InteropKind.Export } e:
                list.Add(new($"{srf.Id}_Broadcast{e.Name}_Serialized", $"{path}.broadcast{e.Name}Serialized"));
                break;
            case PropertyMeta { IK: InteropKind.Import } p:
                if (p.CanGet) list.Add(new($"{srf.Id}_Get{p.Name}_Serialized", $"{path}.get{p.Name}Serialized"));
                if (p.CanSet) list.Add(new($"{srf.Id}_Set{p.Name}_Serialized", $"{path}.set{p.Name}Serialized"));
                break;
            case MethodMeta { IK: InteropKind.Import } m:
                list.Add(new($"{BuildEntry(srf, m)}_Serialized", $"{path}.{m.JSName}Serialized"));
                break;
            case MethodMeta { IK: InteropKind.Export, Async: true } m:
                var entry = BuildEntry(srf, m);
                list.Add(new($"{entry}_Notify", $"{path}.{m.JSName}Notify"));
                list.Add(new($"{entry}_Fail", $"{path}.{m.JSName}Fail"));
                break;
        }
    }

    private static string ResolveModulePath (SurfaceMeta srf, JSModules mds)
    {
        var md = mds.List.First(m => m.Path == srf.JSModule);
        return $"{md.Alias}.{srf.JSNode}";
    }

    private string EmitImport (JSModule md) =>
        $"""import * as {md.Alias} from "./modules/{md.Path}.g.mjs";""";

    private string EmitBinding (JSImportBinding b) =>
        $"bsi.{b.Entry} = (...a) => {b.Handler}(...a);";

    private string EmitLibraryEntry (JSImportBinding b) =>
        $$"""{{b.Entry}}: function () { return BootsharpImports.{{b.Entry}}.apply(null, arguments); }""";
}

/// <summary>
/// Describes a single imported function: the C-ABI entry point name and the JS expression
/// resolving to the user-assigned handler at runtime.
/// </summary>
internal sealed record JSImportBinding (string Entry, string Handler);
