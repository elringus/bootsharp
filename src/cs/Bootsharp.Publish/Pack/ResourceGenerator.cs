namespace Bootsharp.Publish;

internal sealed class ResourceGenerator (string entryAssemblyName, bool embed, bool debug)
{
    private readonly List<string> assemblies = [];
    private readonly List<string> debugging = [];
    private string wasm = null!;

    public string Generate (string buildDir, string debugDir)
    {
        foreach (var path in Directory.GetFiles(buildDir, "*.wasm").Order())
            if (path.EndsWith("dotnet.native.wasm")) wasm = BuildBin(path);
            else assemblies.Add(BuildBin(path));
        if (debug)
        {
            foreach (var path in Directory.GetFiles(debugDir, "*.pdb").Order())
                debugging.Add(BuildBin(path));
            foreach (var path in Directory.GetFiles(debugDir, "*.symbols").Order())
                debugging.Add(BuildBin(path));
        }
        return
            $$"""
              export default {
                  wasm: {{wasm}},
                  assemblies: [
                      {{JoinLines(assemblies, 2, ",\n")}}
                  ],
                  debugging: [
                      {{JoinLines(debugging, 2, ",\n")}}
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
