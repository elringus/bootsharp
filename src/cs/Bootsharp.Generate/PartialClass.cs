using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bootsharp.Generate;

internal sealed class PartialClass (
    Compilation compilation,
    ClassDeclarationSyntax syntax,
    IReadOnlyList<PartialMethod> methods)
{
    public string Name { get; } = syntax.Identifier.ToString();

    public string EmitSource () =>
        """
        #nullable enable
        #pragma warning disable

        """ +
        EmitUsings() +
        WrapNamespace(
            EmitHeader() +
            EmitMethods() +
            EmitFooter()
        );

    private string EmitUsings ()
    {
        var usings = string.Join("\n", syntax.SyntaxTree.GetRoot()
            .DescendantNodesAndSelf().OfType<UsingDirectiveSyntax>());
        return string.IsNullOrEmpty(usings) ? "" : usings + "\n\n";
    }

    private string EmitHeader () => $"{syntax.Modifiers} class {syntax.Identifier}\n{{";

    private string EmitMethods ()
    {
        if (methods.Count == 0) return "";
        var sources = methods.Select(m => "    " + m.EmitSource(compilation));
        return "\n" + string.Join("\n", sources);
    }

    private string EmitFooter () => "\n}";

    private string WrapNamespace (string source)
    {
        if (syntax.Parent is NamespaceDeclarationSyntax space)
            return $$"""
                     namespace {{space.Name}}
                     {
                         {{string.Join("\n", source.Split(["\r\n", "\r", "\n"], StringSplitOptions.None)
                             .Select((s, i) => i > 0 && s.Length > 0 ? "    " + s : s))}}
                     }
                     """;
        if (syntax.Parent is FileScopedNamespaceDeclarationSyntax fileSpace)
            return $"namespace {fileSpace.Name};\n\n{source}";
        return source;
    }
}
