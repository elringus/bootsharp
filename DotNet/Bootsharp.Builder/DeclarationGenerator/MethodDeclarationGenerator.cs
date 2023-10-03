using System.Text;

namespace Bootsharp.Builder;

internal sealed class MethodDeclarationGenerator
{
    private readonly StringBuilder builder = new();

    private Method method => methods[index];
    private Method? prevMethod => index == 0 ? null : methods[index - 1];
    private Method? nextMethod => index == methods.Length - 1 ? null : methods[index + 1];

    private Method[] methods = null!;
    private int index;

    public string Generate (IEnumerable<Method> sourceMethods)
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
        builder.Append($"\n    export function {ToFirstLower(method.Name)}(");
        builder.AppendJoin(", ", method.JSArguments.Select(BuildArgumentDeclaration));
        builder.Append($"): {BuildReturnDeclaration(method)};");
    }

    private void DeclareFunction ()
    {
        builder.Append($"\n    export let {ToFirstLower(method.Name)}: (");
        builder.AppendJoin(", ", method.JSArguments.Select(BuildArgumentDeclaration));
        builder.Append($") => {BuildReturnDeclaration(method)};");
    }

    private void DeclareEvent ()
    {
        builder.Append($"\n    export const {ToFirstLower(method.Name)}: Event<[");
        builder.AppendJoin(", ", method.JSArguments.Select(BuildArgumentDeclaration));
        builder.Append("]>;");
    }

    private string BuildArgumentDeclaration (Argument arg)
    {
        return $"{arg.Name}: {arg.TypeSyntax}{(arg.Nullable ? " | undefined" : "")}";
    }

    private string BuildReturnDeclaration (Method method)
    {
        if (!method.ReturnsNullable) return method.JSReturnTypeSyntax;
        if (!method.ReturnsTaskLike) return $"{method.JSReturnTypeSyntax} | null";
        var insertIndex = method.JSReturnTypeSyntax.Length - 1;
        return method.JSReturnTypeSyntax.Insert(insertIndex, " | null");
    }
}
