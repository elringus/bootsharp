using System;
using System.Collections.Generic;
using System.Linq;

namespace Bootsharp.Packer;

internal sealed class EmbedTemplate
{
    public required string RuntimeWasm { get; init; }
    public required IReadOnlyCollection<Assembly> Assemblies { get; init; }
    public required string EntryAssemblyName { get; init; }

    public string Build () =>
        $$"""
              const bootWithData = exports.boot;
              exports.boot = async function () {
                  const bootData = {
                      wasm: '{{RuntimeWasm}}',
                      assemblies: [{{string.Join(", ", Assemblies.Select(EmbedAssembly))}}],
                      entryAssemblyName: '{{EntryAssemblyName}}'
                  };
                  await bootWithData(bootData);
              };
          """;

    private static string EmbedAssembly (Assembly assembly)
    {
        var base64 = Convert.ToBase64String(assembly.Bytes);
        return $"{{ name: '{assembly.Name}', data: '{base64}' }}";
    }
}
