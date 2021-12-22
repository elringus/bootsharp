using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static Packer.Utilities;

namespace Packer;

internal class TypeGenerator
{
    private readonly Stack<string> declaredAssemblies = new();
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
        var methodsContent = GenerateForMethods(methods);
        var runtimeContent = JoinLines(definitions.Select(GenerateForDefinition), 0);
        return JoinLines(0, runtimeContent, methodsContent) + "\n";
    }

    private string GenerateForMethods (IReadOnlyCollection<Method> methods)
    {
        if (methods.Count == 0) return "";
        var builder = new StringBuilder();
        foreach (var method in methods.OrderBy(m => m.Assembly))
            GenerateForMethod(method, builder);
        return JoinLines(0, builder.ToString(), GenerateAssemblyFooter(declaredAssemblies.Peek()));
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

    private void GenerateForMethod (Method method, StringBuilder builder)
    {
        EnsureWrappedInAssembly(method.Assembly, builder);
        var args = string.Join(", ", method.Arguments.Select(a => $"{a.Name}: {a.Type}"));
        builder.Append($"\n    {method.Name}: ({args}) => {method.ReturnType},");
    }

    private void EnsureWrappedInAssembly (string assembly, StringBuilder builder)
    {
        var prevAssembly = declaredAssemblies.Count > 0 ? declaredAssemblies.Peek() : null;
        declaredAssemblies.Push(assembly);
        if (prevAssembly == assembly) return;
        if (prevAssembly != null) builder.Append('\n').Append(GenerateAssemblyFooter(prevAssembly));
        builder.Append('\n').Append(GenerateAssemblyHeader(assembly)).Append('\n');
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

    private static string GenerateAssemblyHeader (string assembly)
    {
        var declaration = "export declare const";
        foreach (var name in assembly.Split('.'))
            declaration += $" {name}: {{";
        return declaration;
    }

    private static string GenerateAssemblyFooter (string assembly)
    {
        var level = assembly.Count(c => c == '.') + 1;
        return string.Concat(Enumerable.Repeat("};", level));
    }
}
