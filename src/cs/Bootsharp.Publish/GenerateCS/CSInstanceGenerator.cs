namespace Bootsharp.Publish;

/// <summary>
/// Generates C# binding proxies for <see cref="InstanceMeta"/>.
/// Symmetrical to <see cref="JSInstanceGenerator"/>, which generates the same but for the JS side.
/// </summary>
internal sealed class CSInstanceGenerator
{
    private InstanceMeta it = null!;

    public string Generate (IReadOnlyCollection<InstanceMeta> its) =>
        $$"""
          #nullable enable
          #pragma warning disable

          using System.Runtime.CompilerServices;
          using System.Runtime.InteropServices.JavaScript;

          namespace Bootsharp.Generated;

          public static partial class Instances
          {
              internal static int Export<T> (T? it, Bootsharp.Instances.ExportCallback<T>? cb = null) where T : class => Bootsharp.Instances.Export(it, cb);
              internal static T Exported<T> (int id) where T : class => Bootsharp.Instances.Exported<T>(id);
              internal static T Resolve<T> (int id) where T : class => Bootsharp.Instances.Resolve<T>(id);

              internal static void DisposeImported (int id)
              {
                  NotifyImportedDisposed(id);
                  Bootsharp.Instances.DisposeImported(id);
              }

              [ModuleInitializer]
              internal static void RegisterImports ()
              {
                  {{Fmt(its.Where(i => i.IK == InteropKind.Import).Select(EmitImporter), 2)}}
              }

              {{Fmt(its.Where(i => i.Exporter != null).Select(EmitExporter), 1, "\n\n")}}

              [JSExport] private static void DisposeExported (int id) => Bootsharp.Instances.DisposeExported(id);
              [JSImport("instances.disposeImported", "Bootsharp")] private static partial void NotifyImportedDisposed (int id);
          }

          {{Fmt(its.Where(i => i.IK == InteropKind.Import).Select(EmitProxy), 0, "\n\n")}}
          """;

    private static string EmitImporter (InstanceMeta it)
    {
        var proxy = it is DelegateMeta
            ? $"static id => new {it.Syntax}(new {it.Proxy.Syntax}(id).Invoke)"
            : $"static id => new {it.Proxy.Syntax}(id)";
        return $"Bootsharp.Instances.RegisterImport(typeof({it.Syntax}), {proxy});";
    }

    private static string EmitExporter (InstanceMeta it)
    {
        var evt = it.Members.OfType<EventMeta>().ToArray();
        return
            $$"""
              internal static int {{it.Exporter}} ({{it.Syntax}} it) => Export(it, static (_id, it) => {
                  {{Fmt(evt.Select(e => $"it.{e.Name} += Handle{e.Name};"))}}
                  return () => {
                      {{Fmt(evt.Select(e => $"it.{e.Name} -= Handle{e.Name};"), 2)}}
                  };

                  {{Fmt(evt.Select(e => {
                      var args = string.Join(", ", e.Args.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
                      var invArgs = PrependIdArg(string.Join(", ", e.Args.Select(Export)));
                      var name = $"{it.Id}_Broadcast{e.Name}_Serialized";
                      return $"void Handle{e.Name} ({args}) => Interop.{name}({invArgs});";
                  }))}}
              });
              """;
    }

    private string EmitProxy (InstanceMeta it) => (this.it = it) switch {
        DelegateMeta del => EmitDelegateProxy(del),
        _ => EmitOpaqueProxy(it)
    };

    private string EmitOpaqueProxy (InstanceMeta it) =>
        $$"""
          public class {{it.Proxy.Id}} (int id) : global::Bootsharp.JSProxy(id), {{it.Syntax}}
          {
              ~{{it.Proxy.Id}}() => Instances.DisposeImported(_id);

              {{Fmt(it.Members.Select(EmitMemberImport))}}
          }
          """;

    private static string EmitDelegateProxy (DelegateMeta del)
    {
        var inv = del.Invoker;
        var args = string.Join(", ", inv.Args.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var callArgs = PrependIdArg(string.Join(", ", inv.Args.Select(a => a.Name)));
        var fn = $"global::Bootsharp.Generated.Interop.{del.Proxy.Id}_{inv.Name}";
        return
            $$"""
              public sealed class {{del.Proxy.Id}} (int id) : global::Bootsharp.JSProxy(id)
              {
                  ~{{del.Proxy.Id}}() => Instances.DisposeImported(_id);

                  public {{inv.Return.TypeSyntax}} Invoke ({{args}}) => {{fn}}({{callArgs}});
              }
              """;
    }

    private string EmitMemberImport (MemberMeta member) => member switch {
        EventMeta evt => EmitEventImport(evt),
        PropertyMeta prop => EmitPropertyImport(prop),
        _ => EmitMethodImport((MethodMeta)member),
    };

    private string EmitEventImport (EventMeta evt)
    {
        var args = string.Join(", ", evt.Args.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var callArgs = string.Join(", ", evt.Args.Select(a => a.Name));
        return Fmt(0,
            $"public event {evt.TypeSyntax} {evt.Name};",
            $"internal void Invoke{evt.Name} ({args}) => {evt.Name}?.Invoke({callArgs});"
        );
    }

    private string EmitPropertyImport (PropertyMeta prop)
    {
        var space = $"global::Bootsharp.Generated.Interop.{it.Proxy.Id}";
        var getArgs = PrependIdArg("");
        var setArgs = PrependIdArg("value");
        return
            $$"""
              {{prop.TypeSyntax}} {{it.Syntax}}.{{prop.Name}}
              {
                  {{Fmt(
                      prop.CanGet ? $"get => {space}_Get{prop.Name}({getArgs});" : null,
                      prop.CanSet ? $"set => {space}_Set{prop.Name}({setArgs});" : null
                  )}}
              }
              """;
    }

    private string EmitMethodImport (MethodMeta method)
    {
        var args = string.Join(", ", method.Args.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var callArgs = PrependIdArg(string.Join(", ", method.Args.Select(a => a.Name)));
        var name = $"{it.Proxy.Id}_{method.Name}";
        return $"{method.Return.TypeSyntax} {it.Syntax}.{method.Name} ({args}) => " +
               $"global::Bootsharp.Generated.Interop.{name}({callArgs});";
    }
}
