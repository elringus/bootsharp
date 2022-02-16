using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Generator
{
    public class NamespaceConverter
    {
        private const string converterAttributeName = "JSNamespaceAttribute";

        public string Convert (string @namespace, IAssemblySymbol assembly)
        {
            foreach (var attribute in CollectAttributes(assembly))
                @namespace = Convert(@namespace, attribute);
            return @namespace;
        }

        private AttributeData[] CollectAttributes (IAssemblySymbol assembly)
        {
            return assembly.GetAttributes()
                .Where(a => a.AttributeClass?.Name == converterAttributeName &&
                            a.ConstructorArguments.Length == 2).ToArray();
        }

        private string Convert (string @namespace, AttributeData attribute)
        {
            var pattern = attribute.ConstructorArguments[0].Value as string;
            var replacement = attribute.ConstructorArguments[1].Value as string;
            return Regex.Replace(@namespace, pattern!, replacement!);
        }
    }
}
