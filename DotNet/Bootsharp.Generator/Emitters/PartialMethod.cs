using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bootsharp.Generator;

internal sealed class PartialMethod(MethodDeclarationSyntax syntax, bool @event)
{
    public string EmitSource (Compilation compilation)
    {
        var @namespace = GetNamespace(compilation);
        return $"{EmitSignature()} => {EmitBody(@namespace)};";
    }

    private string GetNamespace (Compilation compilation)
    {
        var model = compilation.GetSemanticModel(syntax.SyntaxTree);
        var symbol = model.GetEnclosingSymbol(syntax.SpanStart)!;
        var space = symbol.ContainingNamespace.IsGlobalNamespace ? "Global"
            : string.Join(".", symbol.ContainingNamespace.ConstituentNamespaces);
        return ConvertNamespace(space, symbol.ContainingAssembly);
    }

    private string EmitSignature ()
    {
        return $"{syntax.Modifiers} {syntax.ReturnType} {syntax.Identifier} {syntax.ParameterList}";
    }

    private string EmitBody (string @namespace)
    {
        var invokeMethod = GetInvokeMethod();
        var invokeArgs = GetInvokeArgs(@namespace);
        var handle = @event ? "Event" : "Function";
        return $"{handle}.{invokeMethod}({invokeArgs})";
    }

    private string GetInvokeMethod ()
    {
        if (@event) return "Broadcast";
        var returnType = syntax.ReturnType.ToString();
        return
            returnType is "void" ? "InvokeVoid" :
            returnType is "Task" ? "InvokeVoidAsync" :
            returnType.StartsWith("Task<") ? $"InvokeAsync<{returnType.Substring(5, returnType.Length - 6)}>" :
            $"Invoke<{returnType}>";
    }

    private string GetInvokeArgs (string module)
    {
        var args = $"\"{module}.{ToFirstLower(syntax.Identifier.ToString())}\"";
        if (syntax.ParameterList.Parameters.Count == 0) return args;
        var ids = syntax.ParameterList.Parameters.Select(p => p.Identifier);
        args += $", {string.Join(", ", ids)}";
        return args;
    }
}
