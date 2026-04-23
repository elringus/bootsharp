using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bootsharp.Generate;

internal sealed class ImportClass (Compilation cmp, ClassDeclarationSyntax stx,
    IReadOnlyList<ImportMethod> methods, IReadOnlyList<ImportEvent> events)
{
    public string Name { get; } = stx.Identifier.ToString();

    public string EmitSource () =>
        """
        #nullable enable
        #pragma warning disable

        """ +
        EmitUsings() +
        WrapNamespace(
            EmitHeader() +
            EmitMembers() +
            EmitFooter()
        );

    private string EmitUsings ()
    {
        var usings = string.Join("\n", stx.SyntaxTree.GetRoot()
            .DescendantNodesAndSelf().OfType<UsingDirectiveSyntax>());
        return string.IsNullOrEmpty(usings) ? "" : usings + "\n\n";
    }

    private string EmitHeader ()
    {
        var mods = stx.Modifiers.ToString();
        if (!mods.Contains("unsafe")) mods = mods.Replace("partial", "unsafe partial");
        return $"{mods} class {stx.Identifier}{stx.TypeParameterList}{stx.BaseList}{stx.ConstraintClauses}\n{{";
    }

    private string EmitMembers () => "\n" + string.Join("\n", [
        ..events.Select(e => "    " + e.EmitSource(cmp)),
        ..methods.Select(m => "    " + m.EmitSource(cmp))
    ]);

    private string EmitFooter () => "\n}";

    private string WrapNamespace (string src)
    {
        if (stx.Parent is NamespaceDeclarationSyntax space)
            return $$"""
                     namespace {{space.Name}}
                     {
                         {{string.Join("\n", src.Split(["\r\n", "\r", "\n"], StringSplitOptions.None)
                             .Select((s, i) => i > 0 && s.Length > 0 ? "    " + s : s))}}
                     }
                     """;
        if (stx.Parent is FileScopedNamespaceDeclarationSyntax fileSpace)
            return $"namespace {fileSpace.Name};\n\n{src}";
        return src;
    }
}
