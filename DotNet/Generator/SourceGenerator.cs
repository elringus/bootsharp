using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator
{
    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        public void Initialize (GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute (GeneratorExecutionContext context)
        {
            // https://stackoverflow.com/a/68733955/1202251

            AddExportImport(context, context.Compilation.Assembly);

            if (context.SyntaxContextReceiver is SyntaxReceiver receiver)
                AddPartial(context, receiver);
        }

        private void AddPartial (GeneratorExecutionContext context, SyntaxReceiver receiver)
        {
            for (int i = 0; i < receiver.FunctionClasses.Count; i++)
                context.AddSource($"Functions{i}.g", receiver.FunctionClasses[i].EmitSource(context.Compilation));
            for (int i = 0; i < receiver.EventClasses.Count; i++)
                context.AddSource($"Events{i}.g", receiver.EventClasses[i].EmitSource(context.Compilation));
        }

        private void AddExportImport (GeneratorExecutionContext context, IAssemblySymbol assembly)
        {
            var exportAttribute = assembly.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "JSExportAttribute");
            var importAttribute = assembly.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "JSImportAttribute");

            if (exportAttribute != null)
            {
                var type = exportAttribute.ConstructorArguments[0].Values.Select(v => v.Value).OfType<ITypeSymbol>().First(t => t.TypeKind == TypeKind.Interface);
                var method = type.GetMembers().OfType<IMethodSymbol>().First();
                var space = string.Join(".", type.ContainingNamespace.ConstituentNamespaces);
                var bindingType = $"JS{type.Name.Substring(1)}";
                var handlerType = "global::" + space + "." + type.Name;
                context.AddSource("Export.g", $@"// 11
using DotNetJS;
using Microsoft.JSInterop;

namespace {space};

public class {bindingType}
{{
    private static {handlerType} handler = null!;

    public {bindingType} ({handlerType} handler)
    {{
        {bindingType}.handler = handler;
    }}

    [JSInvokable] public static string GetExportedMethodName () => ""{method.Name}"";
}}
");
            }

            if (importAttribute != null) { }
        }

        private static SyntaxList<UsingDirectiveSyntax> CollectUsings (ISymbol symbol)
        {
            var usings = SyntaxFactory.List<UsingDirectiveSyntax>();
            foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
            foreach (var parent in syntaxRef.GetSyntax().AncestorsAndSelf(false))
                if (parent is NamespaceDeclarationSyntax namespaceSyntax)
                    usings = usings.AddRange(namespaceSyntax.Usings);
                else if (parent is CompilationUnitSyntax unitSyntax)
                    usings = usings.AddRange(unitSyntax.Usings);
            return usings;
        }
    }
}
