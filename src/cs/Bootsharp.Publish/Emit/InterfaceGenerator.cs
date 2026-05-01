namespace Bootsharp.Publish;

/// <summary>
/// Generates implementations for interop interfaces.
/// </summary>
internal sealed class InterfaceGenerator
{
    private IReadOnlyCollection<InterfaceMeta> instanced = [];

    public string Generate (SolutionInspection inspection)
    {
        instanced = inspection.InstancedInterfaces;
        var classes = new HashSet<string>();
        foreach (var i in inspection.StaticInterfaces)
            if (i.Interop == InteropKind.Export) classes.Add(EmitExportClass(i));
            else classes.Add(EmitImportClass(i));
        foreach (var i in inspection.InstancedInterfaces)
            if (i.Interop == InteropKind.Import)
                classes.Add(EmitInstancedImportClass(i));
        return
            $$"""
              #nullable enable
              #pragma warning disable

              namespace Bootsharp.Generated
              {
                  internal static class InterfaceRegistrations
                  {
                      [System.Runtime.CompilerServices.ModuleInitializer]
                      internal static void RegisterInterfaces ()
                      {
                          {{Fmt(inspection.StaticInterfaces.Select(EmitRegistration), 3)}}
                      }
                  }
              }

              {{Fmt(classes, 0, "\n\n")}}
              """;
    }

    private string EmitRegistration (InterfaceMeta i)
    {
        var it = i.Interop == InteropKind.Import
            ? $"new ImportInterface(new {i.FullName}())"
            : $"new ExportInterface(typeof({i.TypeSyntax}), handler => new {i.FullName}(({i.TypeSyntax})handler))";
        var key = i.Interop == InteropKind.Import
            ? $"typeof({i.TypeSyntax})"
            : $"typeof({i.FullName})";
        return $"Interfaces.Register({key}, {it});";
    }

    private string EmitExportClass (InterfaceMeta i) =>
        $$"""
          namespace {{i.Namespace}}
          {
              public class {{i.Name}}
              {
                  private static {{i.TypeSyntax}} handler = null!;

                  public {{i.Name}} ({{i.TypeSyntax}} handler)
                  {
                      {{Fmt([
                          $"{i.Name}.handler = handler;",
                          ..i.Members.OfType<EventMeta>().Select(e => $"handler.{e.Name} += {e.Name}.Invoke;")
                      ], 3)}}
                  }

                  {{Fmt(i.Members.Select(EmitExport), 2)}}
              }
          }
          """;

    private string EmitImportClass (InterfaceMeta i) =>
        $$"""
          namespace {{i.Namespace}}
          {
              public class {{i.Name}} : {{i.TypeSyntax}}
              {
                  {{Fmt(i.Members.Select(m => EmitImport(i, m)), 2)}}
              }
          }
          """;

    private string EmitInstancedImportClass (InterfaceMeta i) =>
        $$"""
          namespace {{i.Namespace}}
          {
              public class {{i.Name}} (global::System.Int32 id) : {{i.TypeSyntax}}
              {
                  internal readonly global::System.Int32 _id = id;

                  ~{{i.Name}}()
                  {
                      global::Bootsharp.Instances.DisposeImported(_id);
                      global::Bootsharp.Generated.Interop.DisposeImportedInstance(_id);
                  }

                  {{Fmt(i.Members.Select(m => EmitImport(i, m)), 2)}}
              }
          }
          """;

    private string EmitExport (MemberMeta member) => member switch {
        EventMeta evt => EmitEventExport(evt),
        PropertyMeta prop => EmitPropertyExport(prop),
        _ => EmitMethodExport((MethodMeta)member)
    };

    private string EmitImport (InterfaceMeta i, MemberMeta member) => member switch {
        EventMeta evt => EmitEventImport(evt),
        PropertyMeta prop => EmitPropertyImport(i, prop),
        _ => EmitMethodImport(i, (MethodMeta)member),
    };

    private string EmitEventExport (EventMeta evt)
    {
        var type = BuildSyntax(evt.Info.EventHandlerType!, GetNullability(evt.Info));
        return $"[Export] public static event {type} {evt.Name};";
    }

    private string EmitEventImport (EventMeta evt)
    {
        var type = BuildSyntax(evt.Info.EventHandlerType!, GetNullability(evt.Info));
        var sigArgs = string.Join(", ", evt.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var args = string.Join(", ", evt.Arguments.Select(a => a.Name));
        return Fmt(0,
            $"public event {type} {evt.Name};",
            $"internal void Invoke{evt.Name} ({sigArgs}) => {evt.Name}?.Invoke({args});"
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

    private string EmitPropertyImport (InterfaceMeta i, PropertyMeta prop)
    {
        var inst = IsInstanced(prop);
        var space = $"global::Bootsharp.Generated.Interop.{prop.Space.Replace('.', '_')}";
        var getArgs = inst ? "_id" : "";
        var setArgs = inst ? "_id, value" : "value";
        return
            $$"""
              {{prop.Value.TypeSyntax}} {{i.TypeSyntax}}.{{prop.Name}}
              {
                  {{Fmt(
                      prop.CanGet ? $"get => {space}_GetProperty{prop.Name}({getArgs});" : null,
                      prop.CanSet ? $"set => {space}_SetProperty{prop.Name}({setArgs});" : null
                  )}}
              }
              """;
    }

    private string EmitMethodExport (MethodMeta method)
    {
        var sigArgs = string.Join(", ", method.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var sig = $"public static {method.Value.TypeSyntax} {method.Name} ({sigArgs})";
        var args = string.Join(", ", method.Arguments.Select(a => a.Name));
        return $"[Export] {sig} => handler.{method.Name}({args});";
    }

    private string EmitMethodImport (InterfaceMeta i, MethodMeta method)
    {
        var sigArgs = string.Join(", ", method.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var args = string.Join(", ", method.Arguments.Select(a => a.Name));
        if (IsInstanced(method)) args = PrependIdArg(args);
        var name = $"{method.Space.Replace('.', '_')}_{method.Name}";
        return $"{method.Value.TypeSyntax} {i.TypeSyntax}.{method.Name} ({sigArgs}) => " +
               $"global::Bootsharp.Generated.Interop.{name}({args});";
    }

    private bool IsInstanced (MemberMeta member)
    {
        return instanced.Any(i => i.Members.Contains(member));
    }
}
