namespace Bootsharp.Publish;

/// <summary>
/// Generates binding wrappers for imported instances and instance-specific export handlers.
/// </summary>
internal sealed class InstanceGenerator
{
    private InstancedMeta it = null!;

    public string Generate (SolutionInspection spec) =>
        $$"""
          #nullable enable
          #pragma warning disable

          using System.Runtime.CompilerServices;
          using System.Runtime.InteropServices.JavaScript;

          namespace Bootsharp.Generated
          {
              public static partial class Instances
              {
                  internal static int Export<T> (T instance, global::System.Func<int, T, global::System.Action>? factory = null) where T : class => global::Bootsharp.Instances.Export(instance, factory);
                  internal static T Exported<T> (int id) where T : class => global::Bootsharp.Instances.Exported<T>(id);
                  internal static T Import<T> (int id, global::System.Func<int, T> factory) where T : class => global::Bootsharp.Instances.Import(id, factory);

                  internal static void DisposeImported (int id)
                  {
                      NotifyImportedDisposed(id);
                      global::Bootsharp.Instances.DisposeImported(id);
                  }

                  {{Fmt(spec.Instanced.Where(i => i.Exporter != null).Select(EmitExporter), 2, "\n\n")}}

                  [JSExport] private static void DisposeExported (int id) => global::Bootsharp.Instances.DisposeExported(id);
                  [JSImport("instances.disposeImported", "Bootsharp")] private static partial void NotifyImportedDisposed (int id);
              }
          }

          {{Fmt(spec.Instanced.Where(i => i.Interop == InteropKind.Import).Select(EmitWrapper), 0, "\n\n")}}
          """;

    private static string EmitExporter (InstancedMeta it)
    {
        var evt = it.Members.OfType<EventMeta>().ToArray();
        return
            $$"""
              internal static int {{it.Exporter}} ({{it.Syntax}} instance) => Export(instance, static (_id, instance) => {
                  {{Fmt(evt.Select(e => $"instance.{e.Name} += Handle{e.Name};"))}}
                  return () => {
                      {{Fmt(evt.Select(e => $"instance.{e.Name} -= Handle{e.Name};"), 2)}}
                  };

                  {{Fmt(evt.Select(e => {
                      var args = string.Join(", ", e.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
                      var invArgs = PrependIdArg(string.Join(", ", e.Arguments.Select(Export)));
                      var name = $"{e.JSSpace.Replace('.', '_')}_Broadcast{e.Name}_Serialized";
                      return $"void Handle{e.Name} ({args}) => Interop.{name}({invArgs});";
                  }))}}
              });
              """;
    }

    private string EmitWrapper (InstancedMeta it) =>
        $$"""
          namespace {{(this.it = it).Namespace}}
          {
              public class {{it.Name}} (global::System.Int32 id) : {{it.Syntax}}
              {
                  internal readonly global::System.Int32 _id = id;

                  ~{{it.Name}}() => Instances.DisposeImported(_id);

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
        var type = BuildSyntax(evt.Info.EventHandlerType!, GetNullity(evt.Info));
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
        var space = $"global::Bootsharp.Generated.Interop.{it.FullName.Replace('.', '_')}";
        var getArgs = PrependIdArg("");
        var setArgs = PrependIdArg("value");
        return
            $$"""
              {{type}} {{it.Syntax}}.{{prop.Name}}
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
        var name = $"{it.FullName.Replace('.', '_')}_{method.Name}";
        return $"{method.Return.TypeSyntax} {it.Syntax}.{method.Name} ({args}) => " +
               $"global::Bootsharp.Generated.Interop.{name}({callArgs});";
    }
}
