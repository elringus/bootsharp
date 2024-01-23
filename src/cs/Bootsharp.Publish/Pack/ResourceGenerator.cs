namespace Bootsharp.Publish;

internal sealed class ResourceGenerator (string entryAssemblyName, bool embed)
{
    private readonly List<string> assemblies = [];
    private string wasm = null!;

    public string Generate (string buildDir)
    {
        foreach (var path in Directory.GetFiles(buildDir, "*.wasm"))
            if (path.EndsWith("dotnet.native.wasm")) wasm = BuildBin(path);
            else assemblies.Add(BuildBin(path));
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

    private string BuildBin (string path)
    {
        var name = Path.GetFileName(path);
        var content = embed ? ToBase64(File.ReadAllBytes(path)) : "undefined";
        return $$"""{ name: "{{name}}", content: {{content}} }""";
    }

    private string ToBase64 (byte[] bytes) => $"\"{Convert.ToBase64String(bytes)}\"";
}
