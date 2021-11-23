using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetJS.Generator
{
    internal class JSFunctionMethod
    {
        public JSFunctionMethod (MethodDeclarationSyntax syntax) { }

        public string EmitSource ()
        {
            return "";
        }
    }
}
