using System.Collections.Generic;
using System.Linq;
using static Bootsharp.Builder.TextUtilities;

namespace Bootsharp.Builder;

internal sealed class BootUrisTemplate
{
    public required string WasmUri { get; init; }
    public required string EntryAssemblyUri { get; init; }
    public required IReadOnlyCollection<Assembly> Assemblies { get; init; }

    public string Build () => JoinLines(1,
        "exports.getBootUris = () => ({", JoinLines(2, true,
            $"wasm: \"{WasmUri}\",",
            $"entryAssembly: \"{EntryAssemblyUri}\",",
            "assemblies: [", JoinLines(EmitAssemblies(), 3, true), "]"),
        "});"
    );

    private IEnumerable<string> EmitAssemblies ()
    {
        return Assemblies.Select(a => $"\"{a.Name}\",");
    }
}
