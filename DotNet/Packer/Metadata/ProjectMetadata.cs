using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DotNetJS.Packer
{
    public class ProjectMetadata
    {
        public IReadOnlyList<Assembly> Assemblies => assemblies;
        public IReadOnlyList<Method> InvokableMethods => invokableMethods;
        public IReadOnlyList<Method> FunctionMethods => functionMethods;

        private readonly List<Assembly> assemblies = new List<Assembly>();
        private readonly List<Method> invokableMethods = new List<Method>();
        private readonly List<Method> functionMethods = new List<Method>();

        public void LoadAssemblies (string directory)
        {
            assemblies.Clear();
            invokableMethods.Clear();
            functionMethods.Clear();
            foreach (var path in Directory.GetFiles(directory, "*.dll"))
                LoadAssembly(path);
        }

        private void LoadAssembly (string filePath)
        {
            var name = Path.GetFileName(filePath);
            var base64 = ReadBase64(filePath);
            assemblies.Add(new Assembly(name, base64));
            TryCollectMethods(filePath);
        }

        private static string ReadBase64 (string filePath)
        {
            var bytes = File.ReadAllBytes(filePath);
            return Convert.ToBase64String(bytes);
        }

        private void TryCollectMethods (string filePath)
        {
            const string invokableAttr = "JSInvokableAttribute";
            const string functionAttr = "JSFunctionAttribute";

            try
            {
                foreach (var method in GetStaticMethods(filePath))
                foreach (var attribute in method.CustomAttributes)
                    if (attribute.AttributeType.Name == invokableAttr)
                        invokableMethods.Add(new Method(method));
                    else if (attribute.AttributeType.Name == functionAttr)
                        functionMethods.Add(new Method(method));
            }
            catch { return; }
        }

        private IEnumerable<MethodInfo> GetStaticMethods (string assemblyPath)
        {
            var fullPath = Path.GetFullPath(assemblyPath);
            var assembly = System.Reflection.Assembly.LoadFrom(fullPath);
            var exported = assembly.GetExportedTypes();
            return exported.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static));
        }
    }
}
