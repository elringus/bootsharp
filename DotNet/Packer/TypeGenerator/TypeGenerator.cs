using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Packer.Utilities;

namespace Packer;

internal class TypeGenerator
{
    private readonly TypeMethodGenerator methodGenerator = new();
    private readonly List<TypeDefinition> definitions = new();

    public void LoadDefinitions (string directory)
    {
        foreach (var path in Directory.GetFiles(directory, "*.d.ts"))
        {
            var fileName = Path.GetFileNameWithoutExtension(path)[..^2];
            var source = File.ReadAllText(path);
            definitions.Add(new TypeDefinition(fileName, source));
        }
    }

    public string Generate (AssemblyInspector inspector)
    {
        var methods = inspector.InvokableMethods.Concat(inspector.FunctionMethods).ToArray();
        var methodsContent = methodGenerator.Generate(methods);
        var runtimeContent = JoinLines(definitions.Select(GenerateForDefinition), 0);
        return JoinLines(0, runtimeContent, methodsContent) + "\n";
    }

    private string GenerateForDefinition (TypeDefinition definition)
    {
        if (!ShouldExportDefinition(definition)) return "";
        var source = definition.Source;
        foreach (var line in SplitLines(source))
            if (line.StartsWith("import"))
                source = source.Replace(line, GetSourceForImportLine(line));
        return ModifyInternalDeclarations(source);
    }

    private static bool ShouldExportDefinition (TypeDefinition definition)
    {
        switch (definition.FileName)
        {
            case "boot":
            case "interop": return true;
            default: return false;
        }
    }

    private string GetSourceForImportLine (string line)
    {
        var importStart = line.LastIndexOf("\"./", StringComparison.Ordinal) + 3;
        var importLength = line.Length - importStart - 2;
        var import = line.Substring(importStart, importLength);
        foreach (var definition in definitions)
            if (definition.FileName == import)
                return definition.Source;
        throw new PackerException($"Failed to find type import for '{import}'.");
    }

    private static string ModifyInternalDeclarations (string source)
    {
        source = source.Replace("boot(bootData: BootData):", "boot():");
        source = source.Replace("export declare function initializeInterop(): void;", "");
        source = source.Replace("export declare function initializeMono(assemblies: Assembly[]): void;", "");
        source = source.Replace("export declare function callEntryPoint(assemblyName: string): Promise<any>;", "");
        return source;
    }
}
