using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using static DotNetJS.Packer.Utilities;

namespace DotNetJS.Packer
{
    public class ProjectMetadata
    {
        public IReadOnlyList<Assembly> Assemblies => assemblies;
        public IReadOnlyList<Method> InvokableMethods => invokableMethods;
        public IReadOnlyList<Method> FunctionMethods => functionMethods;

        private readonly TaskLoggingHelper log;
        private readonly List<Assembly> assemblies = new List<Assembly>();
        private readonly List<Method> invokableMethods = new List<Method>();
        private readonly List<Method> functionMethods = new List<Method>();

        public ProjectMetadata (TaskLoggingHelper log)
        {
            this.log = log;
        }

        public void LoadAssemblies (string directory)
        {
            foreach (var path in Directory.GetFiles(directory, "*.dll"))
                LoadAssembly(path);
            ReportDiscoveredMethods();
        }

        private void LoadAssembly (string assemblyPath)
        {
            var name = Path.GetFileName(assemblyPath);
            var base64 = ReadBase64(assemblyPath);
            assemblies.Add(new Assembly(name, base64));
            if (ShouldInspectAssembly(name))
                InspectAssembly(assemblyPath);
        }

        private static string ReadBase64 (string filePath)
        {
            var bytes = File.ReadAllBytes(filePath);
            return Convert.ToBase64String(bytes);
        }

        private bool ShouldInspectAssembly (string assemblyName)
        {
            if (assemblyName.StartsWith("System.")) return false;
            if (assemblyName.StartsWith("Microsoft.")) return false;
            return true;
        }

        private void InspectAssembly (string assemblyPath)
        {
            try
            {
                foreach (var method in GetStaticMethods(assemblyPath))
                foreach (var attribute in method.CustomAttributes)
                    InspectMethodAttribute(attribute, method);
            }
            catch (Exception e)
            {
                log.LogWarning($"Failed to inspect '{assemblyPath}' assembly; " +
                               $"affected methods won't be available in JavaScript. Error: {e}");
                return;
            }
        }

        private IEnumerable<MethodInfo> GetStaticMethods (string assemblyPath)
        {
            var fullPath = Path.GetFullPath(assemblyPath);
            var assembly = System.Reflection.Assembly.LoadFrom(fullPath);
            var exported = assembly.GetExportedTypes();
            return exported.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static));
        }

        private void InspectMethodAttribute (CustomAttributeData attribute, MethodInfo method)
        {
            if (attribute.AttributeType.Name == Attributes.Invokable)
                invokableMethods.Add(new Method(method));
            else if (attribute.AttributeType.Name == Attributes.Function)
                functionMethods.Add(new Method(method));
        }

        private void ReportDiscoveredMethods ()
        {
            log.LogMessage(MessageImportance.Normal, JoinLines("Discovered JS invokable methods:",
                JoinLines(invokableMethods.Select(m => m.ToString()))));
            log.LogMessage(MessageImportance.Normal, JoinLines("Discovered JS function methods:",
                JoinLines(functionMethods.Select(m => m.ToString()))));
        }
    }
}
