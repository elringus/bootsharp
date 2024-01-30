namespace Bootsharp.Publish;

/// <summary>
/// Generates implementations for interop interfaces.
/// </summary>
internal sealed class InterfaceGenerator
{
    private readonly HashSet<string> classes = [];
    private readonly HashSet<string> registrations = [];
    private HashSet<InterfaceMeta> instanced = [];

    public string Generate (SolutionInspection inspection)
    {
        instanced = inspection.InstancedInterfaces.ToHashSet();
        foreach (var inter in inspection.StaticInterfaces)
            AddInterface(inter);
        foreach (var inter in inspection.InstancedInterfaces)
            if (inter.Kind == InterfaceKind.Import)
                classes.Add(EmitInstancedImportClass(inter));
        return
            $$"""
              #nullable enable
              #pragma warning disable

              {{JoinLines(classes, 0)}}

              namespace Bootsharp.Generated
              {
                  internal static class InterfaceRegistrations
                  {
                      [System.Runtime.CompilerServices.ModuleInitializer]
                      internal static void RegisterInterfaces ()
                      {
                          {{JoinLines(registrations, 3)}}
                      }
                  }
              }
              """;
    }

    private void AddInterface (InterfaceMeta i)
    {
        if (i.Kind == InterfaceKind.Export) classes.Add(EmitExportClass(i));
        else classes.Add(EmitImportClass(i));
        registrations.Add(EmitRegistration(i));
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

                  {{JoinLines(i.Methods.Select(EmitExportMethod), 2)}}
              }
          }
          """;

    private string EmitImportClass (InterfaceMeta i) =>
        $$"""
          namespace {{i.Namespace}}
          {
              public class {{i.Name}} : {{i.TypeSyntax}}
              {
                  {{JoinLines(i.Methods.Select(m => EmitImportMethod(i, m)), 2)}}

                  {{JoinLines(i.Methods.Select(m => EmitImportMethodImplementation(i, m)), 2)}}
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

                  {{JoinLines(i.Methods.Select(m => EmitImportMethod(i, m)), 2)}}

                  {{JoinLines(i.Methods.Select(m => EmitImportMethodImplementation(i, m)), 2)}}
              }
          }
          """;

    private string EmitRegistration (InterfaceMeta i) => i.Kind == InterfaceKind.Import ?
        $"Interfaces.Register(typeof({i.TypeSyntax}), new ImportInterface(new {i.FullName}()));" :
        $"Interfaces.Register(typeof({i.FullName}), new ExportInterface(typeof({i.TypeSyntax}), handler => new {i.FullName}(({i.TypeSyntax})handler)));";

    private string EmitExportMethod (MethodMeta method)
    {
        var sigArgs = string.Join(", ", method.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var sig = $"public static {method.ReturnValue.TypeSyntax} {method.Name} ({sigArgs})";
        var args = string.Join(", ", method.Arguments.Select(a => a.Name));
        return $"[JSInvokable] {sig} => handler.{method.Name}({args});";
    }

    private string EmitImportMethod (InterfaceMeta i, MethodMeta method)
    {
        var attr = method.Kind == MethodKind.Function ? "JSFunction" : "JSEvent";
        var sigArgs = string.Join(", ", method.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        if (instanced.Contains(i)) sigArgs = PrependInstanceIdArgTypeAndName(sigArgs);
        var sig = $"public static {method.ReturnValue.TypeSyntax} {method.Name} ({sigArgs})";
        var args = string.Join(", ", method.Arguments.Select(a => a.Name));
        if (instanced.Contains(i)) args = PrependInstanceIdArgName(args);
        return $"[{attr}] {sig} => {EmitProxyGetter(i, method)}({args});";
    }

    private string EmitImportMethodImplementation (InterfaceMeta i, MethodMeta method)
    {
        var sigArgs = string.Join(", ", method.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var args = string.Join(", ", method.Arguments.Select(a => a.Name));
        if (instanced.Contains(i)) args = PrependInstanceIdArgName(args);
        return $"{method.ReturnValue.TypeSyntax} {i.TypeSyntax}.{method.InterfaceName} ({sigArgs}) => {method.Name}({args});";
    }

    private string EmitProxyGetter (InterfaceMeta i, MethodMeta method)
    {
        var func = method.ReturnValue.Void ? "global::System.Action" : "global::System.Func";
        var syntax = method.Arguments.Select(a => a.Value.TypeSyntax).ToList();
        if (instanced.Contains(i)) syntax.Insert(0, BuildSyntax(typeof(int)));
        if (!method.ReturnValue.Void) syntax.Add(method.ReturnValue.TypeSyntax);
        if (syntax.Count > 0) func = $"{func}<{string.Join(", ", syntax)}>";
        return $"Proxies.Get<{func}>(\"{method.Space}.{method.Name}\")";
    }
}
