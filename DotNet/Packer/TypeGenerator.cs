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
        private readonly List<TypeDefinition> satelliteDefinitions = new List<TypeDefinition>();
        private TypeDefinition mainDefinition;

        public void LoadDefinitions (string directory)
        {
            declaredAssemblies.Clear();
            satelliteDefinitions.Clear();
            mainDefinition = null;
            foreach (var path in Directory.GetFiles(directory, "*.d.ts"))
                LoadDefinition(path);
        }

        public string Generate (ProjectMetadata project)
        {
            var satelliteSources = satelliteDefinitions.Select(GenerateSatellite);
            return GenerateMain(project) + "\n" + string.Join("\n", satelliteSources);
        }

        private void LoadDefinition (string path)
        {
            var definition = CreateDefinition(path);
            if (definition.FileName == "dotnet")
                mainDefinition = definition;
            else satelliteDefinitions.Add(definition);
        }

        private TypeDefinition CreateDefinition (string path)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            fileName = fileName.Substring(0, fileName.Length - 2);
            var source = File.ReadAllText(path);
            return new TypeDefinition(fileName, source);
        }

        private string GenerateMain (ProjectMetadata project)
        {
            var source = mainDefinition.Source;
            foreach (var line in SplitLines(source))
                if (line.StartsWith("import {") || line.StartsWith("export {"))
                    source = source.Replace(line, "");
                else if (line == "export declare const dotnet: {")
                    source = source.Replace(line, JoinLines(line, GenerateBindings(project)));
            return source.Trim();
        }

        private string GenerateBindings (ProjectMetadata project)
        {
            var methods = project.InvokableMethods
                .Concat(project.FunctionMethods)
                .OrderBy(m => m.Assembly).ToArray();
            if (methods.Length == 0) return "";
            return JoinLines(JoinLines(methods.Select(GenerateBinding), 2), "};");
        }

        private string GenerateBinding (Method method)
        {
            var args = string.Join(", ", method.Arguments.Select(a => $"{a.Name}: {a.Type}"));
            var declaration = $"{method.Name}: ({args}) => {method.ReturnType},";
            return EnsureWrappedInAssembly(method.Assembly, declaration);
        }

        private string GenerateSatellite (TypeDefinition definition)
        {
            var source = definition.Source;
            foreach (var line in SplitLines(source))
                if (line.StartsWith("import"))
                    source = source.Replace(line, GetSourceForImportLine(line));
            return source.Replace("boot(bootData: BootData):", "boot():").Trim();
        }

        private string GetSourceForImportLine (string line)
        {
            var importStart = line.LastIndexOf("\"./", StringComparison.Ordinal) + 3;
            var importLength = line.Length - importStart - 2;
            var import = line.Substring(importStart, importLength);
            foreach (var definition in satelliteDefinitions)
                if (definition.FileName == import)
                    return definition.Source;
            throw new InvalidOperationException($"Failed to find type import for '{import}'.");
        }

        private string EnsureWrappedInAssembly (string assembly, string declaration)
        {
            if (declaredAssemblies.Count > 0 && declaredAssemblies.Peek() == assembly) return declaration;
            if (declaredAssemblies.Count > 0) declaration = JoinLines("};", declaration);
            declaredAssemblies.Push(assembly);
            declaration = JoinLines(2, $"{assembly}: {{", declaration);
            return declaration;
        }
    }
}
