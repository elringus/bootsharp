namespace Bootsharp.Publish;

internal sealed class BindingClassGenerator
{
    public string Generate (IReadOnlyCollection<InterfaceMeta> instanced)
    {
        var exported = instanced.Where(i => i.Interop == InteropKind.Export);
        return Fmt(exported.Select(EmitClass), 0) + '\n';
    }

    private string EmitClass (InterfaceMeta it) =>
        $$"""
          class {{BuildJSInstanceClassName(it)}} {
              {{Fmt([
                  "constructor(_id) { this._id = _id; registerExported(this, _id); }",
                  ..it.Members.Select(EmitMember)
              ])}}
          }
          """;

    private string EmitMember (MemberMeta member) => member switch {
        EventMeta evt => EmitEvent(evt),
        PropertyMeta prop => EmitProperty(prop),
        _ => EmitMethod((MethodMeta)member)
    };

    private string EmitEvent (EventMeta evt)
    {
        var args = string.Join(", ", evt.Arguments.Select(a => a.JSName));
        return Fmt(0,
            $"{evt.JSName} = new Event();",
            $"broadcast{evt.Name}({args}) {{ this.{evt.JSName}.broadcast({args}); }}",
            $"static broadcast{evt.Name}({PrependIdArg(args)}) {{ getExported(_id).broadcast{evt.Name}({args}); }}"
        );
    }

    private string EmitMethod (MethodMeta method)
    {
        var sigArgs = string.Join(", ", method.Arguments.Select(a => a.Name));
        var invArgs = sigArgs.Length > 0 ? $"this._id, {sigArgs}" : "this._id";
        var body = $"{method.JSSpace}.{method.JSName}({invArgs})";
        if (!method.Void) body = $"return {body}";
        return $"{method.JSName}({sigArgs}) {{ {body}; }}";
    }

    private string EmitProperty (PropertyMeta p) => Fmt(0,
        p.CanGet ? $"get {p.JSName}() {{ return {p.JSSpace}.getProperty{p.Name}(this._id); }}" : null,
        p.CanSet ? $"set {p.JSName}(value) {{ {p.JSSpace}.setProperty{p.Name}(this._id, value); }}" : null
    );
}
