namespace Bootsharp.Builder;

internal sealed class ImportGenerator
{
    public string Generate (AssemblyInspector inspector)
    {
        var bySpace = inspector.Methods
            .Where(m => m.Type != MethodType.Invokable)
            .GroupBy(i => i.DeclaringName).ToArray();
        return bySpace.Length == 0 ? "" :
            $"""
             using System.Runtime.InteropServices.JavaScript;
             using System.Diagnostics.CodeAnalysis;
             using System.Runtime.CompilerServices;

             namespace Bootsharp.Imports;

             {JoinLines(bySpace.Select(g => GenerateSpace(g.Key, g.ToArray())), 0)}
             """;
    }

    private string GenerateSpace (string space, IReadOnlyList<Method> methods)
    {
        var name = space.Replace('.', '_');
        var asm = methods[0].Assembly;
        return
            $$"""
              public partial class {{name}}
              {
                  [ModuleInitializer]
                  [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Bootsharp.Imports.{{name}}", "{{asm}}")]
                  internal static void RegisterDynamicDependencies ()
                  {
                      {{JoinLines(methods.Select(GenerateFunctionAssign), 2)}}
                  }
                  {{JoinLines(methods.Select(GenerateImport))}}
              }
              """;
    }

    private string GenerateFunctionAssign (Method method)
    {
        return $"""Function.Set("{BuildEndpoint(method)}", {method.Name});""";
    }

    private string GenerateImport (Method method)
    {
        var args = string.Join(", ", method.Arguments.Select(GenerateArg));
        var @return = method.ReturnsVoid ? "void" : (method.ShouldSerializeReturnType
            ? $"global::System.String{(method.ReturnsNullable ? "?" : "")}"
            : method.ReturnType);
        if (method.ShouldSerializeReturnType && method.ReturnsTaskLike)
            @return = $"global::System.Threading.Tasks.Task<{@return}>";
        var attr = $"""[JSImport("{BuildEndpoint(method)}", "Bootsharp")]""";
        return $"{attr} internal static partial {@return} {method.Name} ({args});";
    }

    private string GenerateArg (Argument arg)
    {
        var type = arg.ShouldSerialize
            ? $"global::System.String{(arg.Nullable ? "?" : "")}"
            : arg.Type;
        return $"{type} {arg.Name}";
    }

    private string BuildEndpoint (Method method)
    {
        var space = method.Namespace ?? "Global";
        var name = char.ToLowerInvariant(method.Name[0]) + method.Name[1..];
        return $"{space}.{name}{(method.Type == MethodType.Event ? ".broadcast" : "")}";
    }
}
