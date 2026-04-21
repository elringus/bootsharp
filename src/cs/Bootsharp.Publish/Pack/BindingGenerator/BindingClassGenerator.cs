namespace Bootsharp.Publish;

internal sealed class BindingClassGenerator
{
    public string Generate (IReadOnlyCollection<InterfaceMeta> instanced)
    {
        var exported = instanced.Where(i => i.Interop == InteropKind.Export);
        return JoinLines(exported.Select(EmitClass), 0) + '\n';
    }

    private string EmitClass (InterfaceMeta inter) =>
        $$"""
          class {{BuildJSInteropInstanceClassName(inter)}} {
              constructor(_id) { this._id = _id; disposeOnFinalize(this, _id); }
              {{JoinLines(inter.Members.Select(EmitMember))}}
          }
          """;

    private string EmitMember (MemberMeta member) => member switch {
        PropertyMeta prop => EmitProperty(prop),
        _ => EmitMethod((MethodMeta)member)
    };

    private string EmitMethod (MethodMeta method)
    {
        var sigArgs = string.Join(", ", method.Arguments.Select(a => a.Name));
        var callArgs = sigArgs.Length > 0 ? $"this._id, {sigArgs}" : "this._id";
        var body = $"{method.JSSpace}.{method.JSName}({callArgs})";
        if (!method.Void) body = $"return {body}";
        return $"{method.JSName}({sigArgs}) {{ {body}; }}";
    }

    private string EmitProperty (PropertyMeta p) => JoinLines(0,
        p.CanGet ? $"get {p.JSName}() {{ return {p.JSSpace}.getProperty{p.Name}(this._id); }}" : null,
        p.CanSet ? $"set {p.JSName}(value) {{ {p.JSSpace}.setProperty{p.Name}(this._id, value); }}" : null
    );
}
