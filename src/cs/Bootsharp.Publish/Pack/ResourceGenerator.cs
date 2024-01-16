namespace Bootsharp.Publish;

internal sealed class ResourceGenerator (string entryAssemblyName, string buildDir, bool embed)
{
    public string Generate (AssemblyInspection inspection)
    {
        var wasm = BuildBin("dotnet.native.wasm", GenerateWasm());
        var assemblies = inspection.Assemblies.Select(BuildAssembly);
        return
            $$"""
              export default {
                  wasm: {{wasm}},
                  assemblies: [
                      {{JoinLines(assemblies, 2, ",\n")}}
                  ],
                  entryAssemblyName: "{{entryAssemblyName}}"
              };
              """;
    }

    private string GenerateWasm ()
    {
        if (!embed) return "undefined";
        var path = Path.Combine(buildDir, "dotnet.native.wasm");
        var bytes = File.ReadAllBytes(path);
        return ToBase64(bytes);
    }

    private string BuildAssembly (AssemblyMeta assembly)
    {
        var name = assembly.Name + ".wasm";
        var content = embed ? ToBase64(assembly.Bytes) : "undefined";
        return BuildBin(name, content);
    }

    private string BuildBin (string name, string content)
    {
        return $$"""{ name: "{{name}}", content: {{content}} }""";
    }

    private string ToBase64 (byte[] bytes) => $"\"{Convert.ToBase64String(bytes)}\"";
}
