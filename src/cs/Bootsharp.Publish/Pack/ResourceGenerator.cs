namespace Bootsharp.Publish;

internal sealed class ResourceGenerator (string entryAssemblyName, bool debug, bool g11n)
{
    private readonly List<string> assemblies = [];
    private readonly List<string> symbols = [];
    private readonly List<string> pdb = [];
    private readonly List<string> icu = [];
    private string wasm = null!;

    public string Generate (string buildDir, string debugDir)
    {
        foreach (var path in Directory.GetFiles(buildDir, "*.wasm").Order())
            if (path.EndsWith("dotnet.native.wasm")) wasm = BuildResource(path);
            else assemblies.Add(BuildResource(path));
        if (g11n)
        {
            foreach (var path in Directory.GetFiles(buildDir, "*.dat").Order())
                icu.Add(BuildResource(path));
        }
        if (debug)
        {
            foreach (var path in Directory.GetFiles(debugDir, "*.symbols").Order())
                symbols.Add(BuildResource(path));
            foreach (var path in Directory.GetFiles(debugDir, "*.pdb").Order())
                pdb.Add(BuildResource(path));
        }
        return
            $$"""
              export default {
                  wasm: {{wasm}},
                  assemblies: [
                      {{Fmt(assemblies, 2, ",\n")}}
                  ],
                  icu: [
                      {{Fmt(icu, 2, ",\n")}}
                  ],
                  symbols: [
                      {{Fmt(symbols, 2, ",\n")}}
                  ],
                  pdb: [
                      {{Fmt(pdb, 2, ",\n")}}
                  ],
                  entryAssemblyName: "{{entryAssemblyName}}"
              };
              """;
    }

    private string BuildResource (string path)
    {
        var name = Path.GetFileName(path);
        return $$"""{ name: "{{name}}" }""";
    }
}
