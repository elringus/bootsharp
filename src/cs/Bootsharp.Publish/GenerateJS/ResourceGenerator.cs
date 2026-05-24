namespace Bootsharp.Publish;

/// <summary>
/// Generates a manifest listing resources required to initialize the .NET runtime,
/// optionally embedding the binary content as base64 strings.
/// </summary>
internal sealed class ResourceGenerator (string entryAssemblyName, string packageName, bool debug, bool g11n, bool embed)
{
    private readonly List<string> assemblies = [];
    private readonly List<string> symbols = [];
    private readonly List<string> pdb = [];
    private readonly List<string> icu = [];
    private string wasm = null!;

    public string Generate (string buildDir, string debugDir)
    {
        var wasmName = $"{packageName}.wasm";
        foreach (var path in Directory.GetFiles(buildDir, "*.wasm").Order())
            if (Path.GetFileName(path) == wasmName) wasm = wasmName;
            else assemblies.Add(Path.GetFileName(path));
        if (g11n)
            foreach (var path in Directory.GetFiles(buildDir, "*.dat").Order())
                icu.Add(Path.GetFileName(path));
        if (debug)
        {
            foreach (var path in Directory.GetFiles(debugDir, "*.symbols").Order())
                symbols.Add(Path.GetFileName(path));
            foreach (var path in Directory.GetFiles(debugDir, "*.pdb").Order())
                pdb.Add(Path.GetFileName(path));
        }
        return $"{GenerateManifest()}\n\n{GenerateEmbedded(buildDir, debugDir)}";
    }

    private string GenerateManifest () =>
        $$"""
          export const manifest = {
              wasm: "{{wasm}}",
              assemblies: [
                  {{FmtNames(assemblies)}}
              ],
              icu: [
                  {{FmtNames(icu)}}
              ],
              symbols: [
                  {{FmtNames(symbols)}}
              ],
              pdb: [
                  {{FmtNames(pdb)}}
              ],
              entryAssemblyName: "{{entryAssemblyName}}"
          };
          """;

    private string GenerateEmbedded (string buildDir, string debugDir) => embed ?
        $$"""
          export const embedded = {
              wasm: "{{ReadBase64(buildDir, wasm)}}",
              assemblies: [
                  {{FmtBins(buildDir, assemblies)}}
              ],
              icu: [
                  {{FmtBins(buildDir, icu)}}
              ],
              symbols: [
                  {{FmtBins(debugDir, symbols)}}
              ],
              pdb: [
                  {{FmtBins(debugDir, pdb)}}
              ]
          };
          """ : "export const embedded = undefined;";

    private static string FmtNames (IEnumerable<string> names) =>
        Fmt(names.Select(n => $"\"{n}\""), 2, ",\n");
    private static string FmtBins (string dir, IEnumerable<string> names) =>
        Fmt(names.Select(n => $"{{ name: \"{n}\", content: \"{ReadBase64(dir, n)}\" }}"), 2, ",\n");
    private static string ReadBase64 (string dir, string name) =>
        Convert.ToBase64String(File.ReadAllBytes(Path.Combine(dir, name)));
}
