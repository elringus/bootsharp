namespace Bootsharp.Publish;

internal sealed class ResourceGenerator (string entryName, string buildDir, bool embed)
{
    public string Generate (AssemblyInspection inspection) => new ResourcesTemplate(
        GenerateWasm(),
        inspection.Assemblies.Select(GenerateAssembly),
        entryName
    ).Build();

    private string GenerateWasm ()
    {
        if (!embed) return "undefined";
        var path = Path.Combine(buildDir, "dotnet.native.wasm");
        var bytes = File.ReadAllBytes(path);
        return ToBase64(bytes);
    }

    private AssemblyResource GenerateAssembly (AssemblyMeta assembly) => new(
        assembly.Name[..^3] + "wasm",
        embed ? ToBase64(assembly.Bytes) : "undefined"
    );

    private string ToBase64 (byte[] bytes) => $"\"{Convert.ToBase64String(bytes)}\"";
}
