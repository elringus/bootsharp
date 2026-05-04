namespace Bootsharp.Publish;

/// <summary>
/// Generates interop wrappers for imported instances.
/// </summary>
internal sealed class InstanceGenerator
{
    private InstancedMeta it = null!;

    public string Generate (SolutionInspection spec) =>
        $"""
         #nullable enable
         #pragma warning disable

         {Fmt(spec.Instanced
             .Where(i => i.Interop == InteropKind.Import)
             .Select(EmitWrapper), 0, "\n\n")}
         """;

    private string EmitWrapper (InstancedMeta it) =>
        $$"""
          namespace {{(this.it = it).Namespace}}
          {
              public class {{it.Name}} (global::System.Int32 id) : {{it.Type.Syntax}}
              {
                  internal readonly global::System.Int32 _id = id;

                  ~{{it.Name}}()
                  {
                      global::Bootsharp.Instances.DisposeImported(_id);
                      global::Bootsharp.Generated.Interop.DisposeImportedInstance(_id);
                  }

                  {{Fmt(it.Members.Select(EmitMemberImport), 2)}}
              }
          }
          """;

    private string EmitMemberImport (MemberMeta member) => member switch {
        EventMeta evt => EmitEventImport(evt),
        PropertyMeta prop => EmitPropertyImport(prop),
        _ => EmitMethodImport((MethodMeta)member),
    };

    private string EmitEventImport (EventMeta evt)
    {
        var type = BuildSyntax(evt.Info.EventHandlerType!, GetNullability(evt.Info));
        var args = string.Join(", ", evt.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var callArgs = string.Join(", ", evt.Arguments.Select(a => a.Name));
        return Fmt(0,
            $"public event {type} {evt.Name};",
            $"internal void Invoke{evt.Name} ({args}) => {evt.Name}?.Invoke({callArgs});"
        );
    }

    private string EmitPropertyImport (PropertyMeta prop)
    {
        var type = (prop.GetValue ?? prop.SetValue!).TypeSyntax;
        var space = $"global::Bootsharp.Generated.Interop.{prop.Space.Replace('.', '_')}";
        var getArgs = PrependIdArg("");
        var setArgs = PrependIdArg("value");
        return
            $$"""
              {{type}} {{it.Type.Syntax}}.{{prop.Name}}
              {
                  {{Fmt(
                      prop.CanGet ? $"get => {space}_GetProperty{prop.Name}({getArgs});" : null,
                      prop.CanSet ? $"set => {space}_SetProperty{prop.Name}({setArgs});" : null
                  )}}
              }
              """;
    }

    private string EmitMethodImport (MethodMeta method)
    {
        var args = string.Join(", ", method.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var callArgs = PrependIdArg(string.Join(", ", method.Arguments.Select(a => a.Name)));
        var name = $"{method.Space.Replace('.', '_')}_{method.Name}";
        return $"{method.Return.TypeSyntax} {it.Type.Syntax}.{method.Name} ({args}) => " +
               $"global::Bootsharp.Generated.Interop.{name}({callArgs});";
    }
}
