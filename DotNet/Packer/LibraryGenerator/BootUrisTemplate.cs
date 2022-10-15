using System.Collections.Generic;
using System.Linq;
using static Packer.TextUtilities;

namespace Packer;

internal class BootUrisTemplate
{
    public string WasmUri { get; init; } = null!;
    public string EntryAssemblyUri { get; init; } = null!;
    public IReadOnlyCollection<Assembly> Assemblies { get; init; } = null!;

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
