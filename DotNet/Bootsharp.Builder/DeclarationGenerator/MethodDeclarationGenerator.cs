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
        methods = sourceMethods.OrderBy(m => m.Namespace).ToArray();
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
        return prevMethod.Namespace != method.Namespace;
    }

    private void OpenNamespace ()
    {
        builder.Append($"\nexport namespace {method.Namespace} {{");
    }

    private bool ShouldCloseNamespace ()
    {
        if (nextMethod is null) return true;
        return nextMethod.Namespace != method.Namespace;
    }

    private void CloseNamespace ()
    {
        builder.Append("\n}");
    }

    private void DeclareInvokable ()
    {
        builder.Append($"\n    export function {ToFirstLower(method.Name)}(");
        builder.AppendJoin(", ", method.Arguments.Select(BuildArgumentDeclaration));
        builder.Append($"): {BuildReturnDeclaration(method)};");
    }

    private void DeclareFunction ()
    {
        builder.Append($"\n    export let {ToFirstLower(method.Name)}: (");
        builder.AppendJoin(", ", method.Arguments.Select(BuildArgumentDeclaration));
        builder.Append($") => {BuildReturnDeclaration(method)};");
    }

    private void DeclareEvent ()
    {
        builder.Append($"\n    export const {ToFirstLower(method.Name)}: Event<[");
        builder.AppendJoin(", ", method.Arguments.Select(BuildType));
        builder.Append("]>;");

        string BuildType (Argument arg) => arg.Type + (arg.Nullable ? " | undefined" : "");
    }

    private string BuildArgumentDeclaration (Argument arg)
    {
        return $"{arg.Name + (arg.Nullable ? "?" : "")}: {arg.Type}";
    }

    private string BuildReturnDeclaration (Method method)
    {
        if (!method.ReturnNullable) return method.ReturnType;
        if (!method.Async) return $"{method.ReturnType} | null";
        var insertIndex = method.ReturnType.Length - 1;
        return method.ReturnType.Insert(insertIndex, " | null");
    }
}
