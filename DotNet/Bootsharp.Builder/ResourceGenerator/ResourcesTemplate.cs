namespace Bootsharp.Builder;

internal sealed class ResourcesTemplate(
    string wasm,
    IEnumerable<AssemblyResource> assemblies,
    string entryAssemblyName)
{
    public string Build () =>
        $$"""
          export default {
              wasm: {{BuildBin("dotnet.native.wasm", wasm)}},
              assemblies: [
                  {{JoinLines(assemblies.Select(a => BuildBin(a.Name, a.Content)), 2, ",\n")}}
              ],
              entryAssemblyName: "{{entryAssemblyName}}"
          };
          """;

    private string BuildBin (string name, string content) =>
        $$"""{ name: "{{name}}", content: {{content}} }""";
}
