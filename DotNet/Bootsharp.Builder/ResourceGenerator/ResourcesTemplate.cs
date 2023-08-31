using System.Collections.Generic;
using System.Linq;
using static Bootsharp.Builder.TextUtilities;

namespace Bootsharp.Builder;

internal sealed class ResourcesTemplate(
    string wasm,
    IEnumerable<AssemblyResource> assemblies,
    string entryAssemblyName)
{
    public string Build () =>
        $$"""
          export default {
              wasm: {{wasm}},
              assemblies: [
                  {{JoinLines(assemblies.Select(BuildAssembly), 2, separator: ",\n")}}
              ],
              entryAssemblyName: "{{entryAssemblyName}}"
          };
          """;

    private string BuildAssembly (AssemblyResource assembly) =>
        $$"""{ name: "{{assembly.Name}}", content: {{assembly.Content}} }""";
}
