using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Packer.Utilities;

namespace Packer
{
    public class TypeGenerator
    {
        private readonly Stack<string> declaredAssemblies = new Stack<string>();
        private readonly List<TypeDefinition> definitions = new List<TypeDefinition>();

        public void LoadDefinitions (string directory)
        {
            foreach (var path in Directory.GetFiles(directory, "*.d.ts"))
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                fileName = fileName.Substring(0, fileName.Length - 2);
                var source = File.ReadAllText(path);
                definitions.Add(new TypeDefinition(fileName, source));
            }
        }

        public string Generate (ProjectMetadata project)
        {
            var methods = project.FunctionMethods.Concat(project.InvokableMethods).ToArray();
            var methodsContent = GenerateForMethods(methods);
            var runtimeContent = JoinLines(definitions.Select(GenerateForDefinition), 0);
            return JoinLines(0, runtimeContent, methodsContent) + "\n";
        }

        private string GenerateForMethods (IReadOnlyCollection<Method> methods)
        {
            if (methods.Count == 0) return "";
            var lines = methods.OrderBy(m => m.Assembly).Select(GenerateForMethod);
            return JoinLines(lines) + "\n" + GenerateAssemblyFooter(declaredAssemblies.Peek());
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

        private string GenerateForMethod (Method method)
        {
            var args = string.Join(", ", method.Arguments.Select(a => $"{a.Name}: {a.Type}"));
            var declaration = $"{method.Name}: ({args}) => {method.ReturnType},";
            return EnsureWrappedInAssembly(method.Assembly, declaration);
        }

        private bool ShouldExportDefinition (TypeDefinition definition)
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
            throw new InvalidOperationException($"Failed to find type import for '{import}'.");
        }

        private string ModifyInternalDeclarations (string source)
        {
            source = source.Replace("boot(bootData: BootData):", "boot():");
            source = source.Replace("export declare function initializeInterop(): void;", "");
            source = source.Replace("export declare function initializeMono(assemblies: Assembly[]): void;", "");
            source = source.Replace("export declare function callEntryPoint(assemblyName: string): Promise<any>;", "");
            return source;
        }

        private string EnsureWrappedInAssembly (string assembly, string declaration)
        {
            if (declaredAssemblies.Count > 0 && declaredAssemblies.Peek() == assembly)
                return declaration;
            if (declaredAssemblies.Count > 0)
                declaration = JoinLines(GenerateAssemblyFooter(assembly), declaration);
            declaredAssemblies.Push(assembly);
            declaration = JoinLines(GenerateAssemblyHeader(assembly), declaration);
            return declaration;
        }

        private string GenerateAssemblyHeader (string assembly)
        {
            var declaration = "export declare const";
            foreach (var name in assembly.Split('.'))
                declaration += $" {name}: {{";
            return declaration;
        }

        private string GenerateAssemblyFooter (string assembly)
        {
            var level = assembly.Count(c => c == '.') + 1;
            return string.Concat(Enumerable.Repeat("};", level));
        }
    }
}
