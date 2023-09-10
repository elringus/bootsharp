using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bootsharp.Generator;

internal sealed class PartialMethod(MethodDeclarationSyntax syntax, bool @event)
{
    private readonly bool @void = syntax.ReturnType.ToString() == "void";
    private readonly bool wait = IsResultTask(syntax.ReturnType) && ShouldSerialize(GetTaskResult(syntax.ReturnType));
    private readonly bool resultTask = IsResultTask(syntax.ReturnType);
    private readonly string taskResult = IsResultTask(syntax.ReturnType) ? GetTaskResult(syntax.ReturnType) : null;
    private readonly string result = syntax.ReturnType.ToString();
    private string taskResultOrResult => resultTask ? taskResult : result;

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
        var sig = string.Join(" ", syntax.Modifiers, syntax.ReturnType, syntax.Identifier, syntax.ParameterList);
        if (wait) return sig.Replace("partial", "async partial");
        return sig;
    }

    private string EmitBody (string @namespace)
    {
        var endpoint = GetEndpoint(@namespace);
        var delegateType = GetDelegateType();
        var args = GetArgs();
        var body = $"""Get<{delegateType}>("{endpoint}")({args})""";
        if (!ShouldSerialize(taskResultOrResult)) return body;
        var serialized = resultTask ? taskResult : result;
        return $"Deserialize<{serialized}>({(wait ? "await " : "")}{body})";
    }

    private string GetDelegateType ()
    {
        if (@void && syntax.ParameterList.Parameters.Count == 0) return "Action";
        var name = @void ? "Action" : "Func";
        var args = syntax.ParameterList.Parameters.Select(p => GetDelegateArgType(p.Type));
        if (!@void) args = args.Append(GetDelegateReturnType(syntax.ReturnType));
        return $"{name}<{string.Join(", ", args)}>";
    }

    private string GetDelegateArgType (TypeSyntax type) =>
        ShouldSerialize(type.ToString()) ? "string" : type.ToString();

    private string GetDelegateReturnType (TypeSyntax type) =>
        ShouldSerialize(taskResultOrResult) ?
            (resultTask ? "Task<string>" : "string") : result;

    private string GetEndpoint (string module)
    {
        var name = ToFirstLower(syntax.Identifier.ToString());
        if (@event) name += ".broadcast";
        return $"{module}.{name}";
    }

    private string GetArgs ()
    {
        if (syntax.ParameterList.Parameters.Count == 0) return "";
        var ids = syntax.ParameterList.Parameters.Select(GetArg);
        return string.Join(", ", ids);
    }

    private string GetArg (ParameterSyntax param)
    {
        var type = param.Type!.ToString();
        return ShouldSerialize(type)
            ? $"Serialize({param.Identifier})"
            : param.Identifier.ToString();
    }

    // see table at https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/import-export-interop
    private static bool ShouldSerialize (string type) => type.TrimEnd('?') is not
        ("void" or "bool" or "byte" or "char" or "short" or "long" or "int" or "float" or "double" or
        "Task" or "IntPtr" or "DateTime" or "DateTimeOffset" or "string" or
        "byte[]" or "int[]" or "double[]" or "string[]");

    private static bool IsResultTask (TypeSyntax type)
    {
        return type.ToString().StartsWith("Task<");
    }

    private static string GetTaskResult (TypeSyntax type)
    {
        var name = type.ToString();
        return name.Substring(5, name.Length - 6);
    }
}
