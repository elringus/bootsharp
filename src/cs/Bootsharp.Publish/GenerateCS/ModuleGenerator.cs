namespace Bootsharp.Publish;

/// <summary>
/// Generates implementations for interop modules.
/// </summary>
internal sealed class ModuleGenerator
{
    private ModuleMeta md = null!;

    public string Generate (IReadOnlyCollection<ModuleMeta> mds) =>
        $$"""
          #nullable enable
          #pragma warning disable

          namespace Bootsharp.Generated
          {
              internal static class ModuleRegistrations
              {
                  [System.Runtime.CompilerServices.ModuleInitializer]
                  internal static void RegisterModules ()
                  {
                      {{Fmt(mds.Select(EmitRegistration), 3)}}
                  }
              }
          }

          {{Fmt(mds.Select(EmitModule), 0, "\n\n")}}
          """;

    private string EmitRegistration (ModuleMeta md)
    {
        var type = md.IK == InteropKind.Import
            ? $"typeof({md.Syntax})"
            : $"typeof({md.Proxy.Syntax})";
        var factory = md.IK == InteropKind.Import
            ? $"new ImportModule(new {md.Proxy.Syntax}())"
            : $"new ExportModule(typeof({md.Syntax}), handler => new {md.Proxy.Syntax}(({md.Syntax})handler))";
        return $"Modules.Register({type}, {factory});";
    }

    private string EmitModule (ModuleMeta md)
    {
        this.md = md;
        if (md.IK == InteropKind.Export) return EmitModuleExport();
        return EmitModuleImport();
    }

    private string EmitModuleExport () =>
        $$"""
          namespace {{md.Proxy.Space}}
          {
              public class {{md.Proxy.Name}}
              {
                  private static {{md.Syntax}} handler = null!;

                  public {{md.Proxy.Name}} ({{md.Syntax}} handler)
                  {
                      {{Fmt([
                          $"{md.Proxy.Name}.handler = handler;",
                          ..md.Members.OfType<EventMeta>().Select(e => $"handler.{e.Name} += {e.Name}.Invoke;")
                      ], 3)}}
                  }

                  {{Fmt(md.Members.Select(EmitMemberExport), 2)}}
              }
          }
          """;

    private string EmitModuleImport () =>
        $$"""
          namespace {{md.Proxy.Space}}
          {
              public class {{md.Proxy.Name}} : {{md.Syntax}}
              {
                  {{Fmt(md.Members.Select(EmitMemberImport), 2)}}
              }
          }
          """;

    private string EmitMemberExport (MemberMeta member) => member switch {
        EventMeta evt => EmitEventExport(evt),
        PropertyMeta prop => EmitPropertyExport(prop),
        _ => EmitMethodExport((MethodMeta)member)
    };

    private string EmitMemberImport (MemberMeta member) => member switch {
        EventMeta evt => EmitEventImport(evt),
        PropertyMeta prop => EmitPropertyImport(prop),
        _ => EmitMethodImport((MethodMeta)member)
    };

    private string EmitEventExport (EventMeta evt)
    {
        return $"[Export] public static event {evt.TypeSyntax} {evt.Name};";
    }

    private string EmitEventImport (EventMeta evt)
    {
        var args = string.Join(", ", evt.Args.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var callArgs = string.Join(", ", evt.Args.Select(a => a.Name));
        return Fmt(0,
            $"public event {evt.TypeSyntax} {evt.Name};",
            $"internal void Invoke{evt.Name} ({args}) => {evt.Name}?.Invoke({callArgs});"
        );
    }

    private string EmitPropertyExport (PropertyMeta prop)
    {
        var name = prop.Name;
        var type = prop.TypeSyntax;
        var get = $"[Export] public static {type} Get{name} () => handler.{name};";
        var set = $"[Export] public static void Set{name} ({type} value) => handler.{name} = value;";
        return Fmt(0, prop.CanGet ? get : null, prop.CanSet ? set : null);
    }

    private string EmitPropertyImport (PropertyMeta prop)
    {
        var space = $"global::Bootsharp.Generated.Interop.{md.Proxy.Id}";
        return
            $$"""
              {{prop.TypeSyntax}} {{md.Syntax}}.{{prop.Name}}
              {
                  {{Fmt(
                      prop.CanGet ? $"get => {space}_Get{prop.Name}();" : null,
                      prop.CanSet ? $"set => {space}_Set{prop.Name}(value);" : null
                  )}}
              }
              """;
    }

    private string EmitMethodExport (MethodMeta method)
    {
        var args = string.Join(", ", method.Args.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var sig = $"public static {method.Return.TypeSyntax} {method.Name} ({args})";
        var callArgs = string.Join(", ", method.Args.Select(a => a.Name));
        return $"[Export] {sig} => handler.{method.Name}({callArgs});";
    }

    private string EmitMethodImport (MethodMeta method)
    {
        var args = string.Join(", ", method.Args.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var callArgs = string.Join(", ", method.Args.Select(a => a.Name));
        var name = $"{md.Proxy.Id}_{method.Name}";
        return $"{method.Return.TypeSyntax} {md.Syntax}.{method.Name} ({args}) => " +
               $"global::Bootsharp.Generated.Interop.{name}({callArgs});";
    }
}
