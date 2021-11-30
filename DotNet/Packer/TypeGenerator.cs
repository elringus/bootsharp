using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static DotNetJS.Packer.Utilities;

namespace DotNetJS.Packer
{
    public class TypeGenerator
    {
        private readonly Stack<string> declaredAssemblies = new Stack<string>();
        private readonly List<TypeDefinition> definitions = new List<TypeDefinition>();

        public void LoadDefinitions (string directory)
        {
            foreach (var path in Directory.GetFiles(directory, "*.d.ts"))
                definitions.Add(LoadDefinition(path));
        }

        public string Generate (ProjectMetadata project)
        {
            var projectTypes = GenerateForProject(project);
            var runtimeTypes = JoinLines(definitions.Select(GenerateForDefinition), 0);
            return JoinLines(0, runtimeTypes, projectTypes);
        }

        private TypeDefinition LoadDefinition (string path)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            fileName = fileName.Substring(0, fileName.Length - 2);
            var source = File.ReadAllText(path);
            return new TypeDefinition(fileName, source);
        }

        private string GenerateForProject (ProjectMetadata project)
        {
            var methods = project.InvokableMethods
                .Concat(project.FunctionMethods)
                .OrderBy(m => m.Assembly).ToArray();
            if (methods.Length == 0) return "";
            return JoinLines(JoinLines(methods.Select(GenerateForMethod), 2), "};");
        }

        private string GenerateForMethod (Method method)
        {
            var args = string.Join(", ", method.Arguments.Select(a => $"{a.Name}: {a.Type}"));
            var declaration = $"{method.Name}: ({args}) => {method.ReturnType},";
            return EnsureWrappedInAssembly(method.Assembly, declaration);
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
            if (declaredAssemblies.Count > 0 && declaredAssemblies.Peek() == assembly) return declaration;
            if (declaredAssemblies.Count > 0) declaration = JoinLines("};", declaration);
            declaredAssemblies.Push(assembly);
            declaration = JoinLines(2, $"export declare const {assembly}: {{", declaration);
            return declaration;
        }
    }
}
