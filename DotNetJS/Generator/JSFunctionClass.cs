using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetJS.Generator
{
    internal class JSFunctionClass
    {
        private readonly List<JSFunctionMethod> methods;

        public JSFunctionClass (ClassDeclarationSyntax classSyntax, IEnumerable<JSFunctionMethod> methods)
        {
            this.methods = methods.ToList();
            Log.Add($"Will emit class '{classSyntax.Identifier}' with {this.methods.Count} members.");
        }

        public string EmitSource () => $@"
    {EmitMethods()}
";

        private string EmitMethods ()
        {
            var sources = methods.Select(m => m.EmitSource());
            return string.Join('\n', sources);
        }
    }
}
