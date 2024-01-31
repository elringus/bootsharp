namespace Bootsharp.Publish;

internal sealed class BindingClassGenerator
{
    public string Generate (IReadOnlyCollection<InterfaceMeta> instanced)
    {
        var exported = instanced.Where(i => i.Kind == InterfaceKind.Export);
        return JoinLines(exported.Select(BuildClass), 0);
    }

    private string BuildClass (InterfaceMeta inter) =>
        $$"""
          class {{inter.FullName.Replace("Bootsharp.Generated.Exports.", "").Replace(".", "_")}} {
              constructor(_id) { this._id = _id; disposeOnFinalize(this); }
              {{JoinLines(inter.Methods.Select(BuildFunction))}}
          }
          """;

    private string BuildFunction (MethodMeta inv)
    {
        var sigArgs = string.Join(", ", inv.Arguments.Select(a => a.Name));
        var args = "this._id" + (sigArgs.Length > 0 ? $", {sigArgs}" : "");
        var @return = inv.ReturnValue.Void ? "" : "return ";
        return $"{inv.JSName}({sigArgs}) {{ {@return}{inv.JSSpace}.{inv.JSName}({args}); }}";
    }
}
