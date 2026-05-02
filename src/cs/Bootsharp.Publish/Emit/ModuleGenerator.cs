namespace Bootsharp.Publish;

/// <summary>
/// Generates implementations for interop modules and wrappers for instanced interfaces.
/// </summary>
internal sealed class ModuleGenerator
{
    private InterfaceMeta it = null!;

    public string Generate (SolutionInspection inspection) =>
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
                      {{Fmt(inspection.Modules.Select(EmitRegistration), 3)}}
                  }
              }
          }

          {{Fmt(inspection.Modules.Select(EmitModule), 0, "\n\n")}}
          """;

    private string EmitRegistration (InterfaceMeta it)
    {
        var type = it.Interop == InteropKind.Import
            ? $"typeof({it.TypeSyntax})"
            : $"typeof({it.FullName})";
        var factory = it.Interop == InteropKind.Import
            ? $"new ImportModule(new {it.FullName}())"
            : $"new ExportModule(typeof({it.TypeSyntax}), handler => new {it.FullName}(({it.TypeSyntax})handler))";
        return $"Modules.Register({type}, {factory});";
    }

    private string EmitModule (InterfaceMeta it)
    {
        this.it = it;
        if (it.Interop == InteropKind.Export) return EmitModuleExport();
        return EmitModuleImport();
    }

    private string EmitModuleExport () =>
        $$"""
          namespace {{it.Namespace}}
          {
              public class {{it.Name}}
              {
                  private static {{it.TypeSyntax}} handler = null!;

                  public {{it.Name}} ({{it.TypeSyntax}} handler)
                  {
                      {{Fmt([
                          $"{it.Name}.handler = handler;",
                          ..it.Members.OfType<EventMeta>().Select(e => $"handler.{e.Name} += {e.Name}.Invoke;")
                      ], 3)}}
                  }

                  {{Fmt(it.Members.Select(EmitMemberExport), 2)}}
              }
          }
          """;

    private string EmitModuleImport () =>
        $$"""
          namespace {{it.Namespace}}
          {
              public class {{it.Name}} : {{it.TypeSyntax}}
              {
                  {{Fmt(it.Members.Select(EmitMemberImport), 2)}}
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
        _ => EmitMethodImport((MethodMeta)member),
    };

    private string EmitEventExport (EventMeta evt)
    {
        var type = BuildSyntax(evt.Info.EventHandlerType!, GetNullability(evt.Info));
        return $"[Export] public static event {type} {evt.Name};";
    }

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

    private string EmitPropertyExport (PropertyMeta prop)
    {
        var name = prop.Name;
        var type = prop.Value.TypeSyntax;
        var get = $"[Export] public static {type} GetProperty{name} () => handler.{name};";
        var set = $"[Export] public static void SetProperty{name} ({type} value) => handler.{name} = value;";
        return Fmt(0, prop.CanGet ? get : null, prop.CanSet ? set : null);
    }

    private string EmitPropertyImport (PropertyMeta prop)
    {
        var space = $"global::Bootsharp.Generated.Interop.{prop.Space.Replace('.', '_')}";
        return
            $$"""
              {{prop.Value.TypeSyntax}} {{it.TypeSyntax}}.{{prop.Name}}
              {
                  {{Fmt(
                      prop.CanGet ? $"get => {space}_GetProperty{prop.Name}();" : null,
                      prop.CanSet ? $"set => {space}_SetProperty{prop.Name}(value);" : null
                  )}}
              }
              """;
    }

    private string EmitMethodExport (MethodMeta method)
    {
        var args = string.Join(", ", method.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var sig = $"public static {method.Value.TypeSyntax} {method.Name} ({args})";
        var callArgs = string.Join(", ", method.Arguments.Select(a => a.Name));
        return $"[Export] {sig} => handler.{method.Name}({callArgs});";
    }

    private string EmitMethodImport (MethodMeta method)
    {
        var args = string.Join(", ", method.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var callArgs = string.Join(", ", method.Arguments.Select(a => a.Name));
        var name = $"{method.Space.Replace('.', '_')}_{method.Name}";
        return $"{method.Value.TypeSyntax} {it.TypeSyntax}.{method.Name} ({args}) => " +
               $"global::Bootsharp.Generated.Interop.{name}({callArgs});";
    }
}
