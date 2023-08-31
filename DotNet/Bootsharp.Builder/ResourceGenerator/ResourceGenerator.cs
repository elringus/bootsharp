using System;
using System.IO;
using System.Linq;

namespace Bootsharp.Builder;

internal sealed class ResourceGenerator(
    AssemblyInspector inspector,
    string entryAssemblyName,
    string buildDirectory,
    bool embedBinaries)
{
    public string Generate () => new ResourcesTemplate(
        GenerateWasm(),
        inspector.Assemblies.Select(GenerateAssembly),
        entryAssemblyName
    ).Build();

    private string GenerateWasm ()
    {
        if (!embedBinaries) return "undefined";
        var path = Path.Combine(buildDirectory, "dotnet.native.wasm");
        var bytes = File.ReadAllBytes(path);
        return ToBase64(bytes);
    }

    private AssemblyResource GenerateAssembly (Assembly assembly) => new(
        assembly.Name[..^3] + "wasm",
        embedBinaries ? ToBase64(assembly.Bytes) : "undefined"
    );

    private string ToBase64 (byte[] bytes) => $"\"{Convert.ToBase64String(bytes)}\"";
}
