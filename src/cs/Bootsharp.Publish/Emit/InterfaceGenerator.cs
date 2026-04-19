namespace Bootsharp.Publish;

/// <summary>
/// Generates implementations for interop interfaces.
/// </summary>
internal sealed class InterfaceGenerator
{
    public string Generate (SolutionInspection inspection)
    {
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
                          {{JoinLines(inspection.StaticInterfaces.Select(EmitRegistration), 3)}}
                      }
                  }
              }

              {{JoinLines(classes, 0, "\n\n")}}
              """;
    }

    private string EmitRegistration (InterfaceMeta i)
    {
        var inter = i.Interop == InteropKind.Import
            ? $"new ImportInterface(new {i.FullName}())"
            : $"new ExportInterface(typeof({i.TypeSyntax}), handler => new {i.FullName}(({i.TypeSyntax})handler))";
        var key = i.Interop == InteropKind.Import
            ? $"typeof({i.TypeSyntax})"
            : $"typeof({i.FullName})";
        return $"Interfaces.Register({key}, {inter});";
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
                      {{i.Name}}.handler = handler;
                  }

                  {{JoinLines(i.Members.Select(EmitExport), 2)}}
              }
          }
          """;

    private string EmitImportClass (InterfaceMeta i) =>
        $$"""
          namespace {{i.Namespace}}
          {
              public class {{i.Name}} : {{i.TypeSyntax}}
              {
                  {{JoinLines(i.Members.Select(m => EmitImport(i, m)), 2)}}
              }
          }
          """;

    private string EmitInstancedImportClass (InterfaceMeta i) =>
        $$"""
          namespace {{i.Namespace}}
          {
              public class {{i.Name}}(global::System.Int32 _id) : {{i.TypeSyntax}}
              {
                  ~{{i.Name}}() => global::Bootsharp.Generated.Interop.DisposeImportedInstance(_id);

                  {{JoinLines(i.Members.Select(m => EmitInstancedImport(i, m)), 2)}}
              }
          }
          """;

    private string EmitExport (MemberMeta member) => member switch {
        PropertyMeta prop => EmitPropertyExport(prop),
        _ => EmitMethodExport((MethodMeta)member)
    };

    private string EmitImport (InterfaceMeta i, MemberMeta member) => member switch {
        PropertyMeta prop => EmitPropertyImport(i, prop),
        _ => EmitMethodImport(i, (MethodMeta)member),
    };

    private string EmitInstancedImport (InterfaceMeta i, MemberMeta member) => member switch {
        PropertyMeta prop => EmitInstancedPropertyImport(i, prop),
        _ => EmitInstancedMethodImport(i, (MethodMeta)member)
    };

    private string EmitPropertyExport (PropertyMeta prop)
    {
        var name = prop.Name;
        var type = prop.Value.TypeSyntax;
        var get = $"[JSInvokable] public static {type} GetProperty{name} () => handler.{name};";
        var set = $"[JSInvokable] public static void SetProperty{name} ({type} value) => handler.{name} = value;";
        return JoinLines(0, prop.CanGet ? get : null, prop.CanSet ? set : null);
    }

    private string EmitMethodExport (MethodMeta method)
    {
        var sigArgs = string.Join(", ", method.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var sig = $"public static {method.Value.TypeSyntax} {method.Name} ({sigArgs})";
        var args = string.Join(", ", method.Arguments.Select(a => a.Name));
        return $"[JSInvokable] {sig} => handler.{method.Name}({args});";
    }

    private string EmitPropertyImport (InterfaceMeta i, PropertyMeta prop)
    {
        var space = $"global::Bootsharp.Generated.Interop.Proxy_{prop.Space.Replace('.', '_')}";
        return
            $$"""
              {{prop.Value.TypeSyntax}} {{i.TypeSyntax}}.{{prop.Name}}
              {
                  {{JoinLines(
                      prop.CanGet ? $"get => {space}_GetProperty{prop.Name}();" : null,
                      prop.CanSet ? $"set => {space}_SetProperty{prop.Name}(value);" : null
                  )}}
              }
              """;
    }

    private string EmitMethodImport (InterfaceMeta i, MethodMeta method)
    {
        var sigArgs = string.Join(", ", method.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var contract = method is EventMeta @event ? @event.MethodName : method.Name;
        var args = string.Join(", ", method.Arguments.Select(a => a.Name));
        var name = $"Proxy_{method.Space.Replace('.', '_')}_{method.Name}";
        return $"{method.Value.TypeSyntax} {i.TypeSyntax}.{contract} ({sigArgs}) => " +
               $"global::Bootsharp.Generated.Interop.{name}({args});";
    }

    private string EmitInstancedPropertyImport (InterfaceMeta i, PropertyMeta prop)
    {
        var space = $"global::Bootsharp.Generated.Interop.Proxy_{prop.Space.Replace('.', '_')}";
        return
            $$"""
              {{prop.Value.TypeSyntax}} {{i.TypeSyntax}}.{{prop.Name}}
              {
                  {{JoinLines(
                      prop.CanGet ? $"get => {space}_GetProperty{prop.Name}(_id);" : null,
                      prop.CanSet ? $"set => {space}_SetProperty{prop.Name}(_id, value);" : null
                  )}}
              }
              """;
    }

    private string EmitInstancedMethodImport (InterfaceMeta i, MethodMeta method)
    {
        var sigArgs = string.Join(", ", method.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var contract = method is EventMeta @event ? @event.MethodName : method.Name;
        var args = PrependInstanceIdArgName(string.Join(", ", method.Arguments.Select(a => a.Name)));
        var name = $"Proxy_{method.Space.Replace('.', '_')}_{method.Name}";
        return $"{method.Value.TypeSyntax} {i.TypeSyntax}.{contract} ({sigArgs}) => " +
               $"global::Bootsharp.Generated.Interop.{name}({args});";
    }
}
