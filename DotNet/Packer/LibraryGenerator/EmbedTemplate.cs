using System.Collections.Generic;
using System.Linq;

namespace Packer;

internal class EmbedTemplate
{
    public string RuntimeWasm { get; init; } = null!;
    public IReadOnlyCollection<Assembly> Assemblies { get; init; } = null!;
    public string EntryAssemblyName { get; init; } = null!;

    public string Build () => $@"
    const bootWithData = exports.boot;
    exports.boot = async function () {{
        const bootData = {{
            wasm: '{RuntimeWasm}',
            assemblies: [{string.Join(", ", Assemblies.Select(EmbedAssembly))}],
            entryAssemblyName: '{EntryAssemblyName}'
        }};
        await bootWithData(bootData);
    }};";

    private static string EmbedAssembly (Assembly assembly)
    {
        return $"{{ name: '{assembly.Name}', data: '{assembly.Base64}' }}";
    }
}
