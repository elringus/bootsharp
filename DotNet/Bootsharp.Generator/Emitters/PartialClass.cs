using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Bootsharp.Generator.Common;

namespace Bootsharp.Generator;

internal sealed class PartialClass(ClassDeclarationSyntax syntax, IReadOnlyList<PartialMethod> methods)
{
    public string Name { get; } = syntax.Identifier.ToString();

    public string EmitSource (Compilation compilation) => EmitCommon(
        EmitUsings() +
        WrapNamespace(
            EmitHeader() +
            EmitMethods(compilation) +
            EmitFooter()
        )
    );

    private string EmitUsings ()
    {
        var imports = syntax.SyntaxTree.GetRoot().DescendantNodesAndSelf()
            .OfType<UsingDirectiveSyntax>().Where(u => u.Name?.ToString() != "Bootsharp");
        var result = string.Join("\n", imports);
        return string.IsNullOrEmpty(result) ? "" : result + "\n\n";
    }

    private string EmitHeader () => $"{syntax.Modifiers} class {syntax.Identifier}\n{{\n";

    private string EmitMethods (Compilation compilation)
    {
        var sources = methods.Select(m => "    " + m.EmitSource(compilation));
        return string.Join("\n", sources);
    }

    private string EmitFooter () => "\n}";

    private string WrapNamespace (string source)
    {
        if (syntax.Parent is NamespaceDeclarationSyntax space)
            return $$"""
                     namespace {{space.Name}}
                     {
                         {{string.Join("\n    ", source.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))}}
                     }
                     """;
        if (syntax.Parent is FileScopedNamespaceDeclarationSyntax fileSpace)
            return $"namespace {fileSpace.Name};\n\n{source}";
        return source;
    }
}
