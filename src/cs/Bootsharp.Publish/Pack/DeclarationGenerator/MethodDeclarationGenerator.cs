using System.Text;

namespace Bootsharp.Publish;

internal sealed class MethodDeclarationGenerator
{
    private readonly StringBuilder builder = new();

    private MethodMeta method => methods[index];
    private MethodMeta? prevMethod => index == 0 ? null : methods[index - 1];
    private MethodMeta? nextMethod => index == methods.Length - 1 ? null : methods[index + 1];

    private MethodMeta[] methods = null!;
    private int index;

    public string Generate (IEnumerable<MethodMeta> sourceMethods)
    {
        methods = sourceMethods.OrderBy(m => m.JSSpace).ToArray();
        for (index = 0; index < methods.Length; index++)
            DeclareMethod();
        return builder.ToString();
    }

    private void DeclareMethod ()
    {
        if (ShouldOpenNamespace()) OpenNamespace();
        if (method.Type == MethodType.Invokable) DeclareInvokable();
        else if (method.Type == MethodType.Function) DeclareFunction();
        else DeclareEvent();
        if (ShouldCloseNamespace()) CloseNamespace();
    }

    private bool ShouldOpenNamespace ()
    {
        if (prevMethod is null) return true;
        return prevMethod.JSSpace != method.JSSpace;
    }

    private void OpenNamespace ()
    {
        builder.Append($"\nexport namespace {method.JSSpace} {{");
    }

    private bool ShouldCloseNamespace ()
    {
        if (nextMethod is null) return true;
        return nextMethod.JSSpace != method.JSSpace;
    }

    private void CloseNamespace ()
    {
        builder.Append("\n}");
    }

    private void DeclareInvokable ()
    {
        builder.Append($"\n    export function {method.JSName}(");
        builder.AppendJoin(", ", method.Arguments.Select(BuildArgumentDeclaration));
        builder.Append($"): {BuildReturnDeclaration(method)};");
    }

    private void DeclareFunction ()
    {
        builder.Append($"\n    export let {method.JSName}: (");
        builder.AppendJoin(", ", method.Arguments.Select(BuildArgumentDeclaration));
        builder.Append($") => {BuildReturnDeclaration(method)};");
    }

    private void DeclareEvent ()
    {
        builder.Append($"\n    export const {method.JSName}: Event<[");
        builder.AppendJoin(", ", method.Arguments.Select(BuildArgumentDeclaration));
        builder.Append("]>;");
    }

    private string BuildArgumentDeclaration (ArgumentMeta arg)
    {
        return $"{arg.JSName}: {arg.Value.JSTypeSyntax}{(arg.Value.Nullable ? " | undefined" : "")}";
    }

    private string BuildReturnDeclaration (MethodMeta method)
    {
        if (!method.ReturnValue.Nullable) return method.ReturnValue.JSTypeSyntax;
        if (!method.ReturnValue.Async) return $"{method.ReturnValue.JSTypeSyntax} | null";
        var insertIndex = method.ReturnValue.JSTypeSyntax.Length - 1;
        return method.ReturnValue.JSTypeSyntax.Insert(insertIndex, " | null");
    }
}
