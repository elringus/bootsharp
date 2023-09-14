using Microsoft.CodeAnalysis;

namespace Bootsharp.Generator;

internal class BindingEmitter(IMethodSymbol method, bool @event, string space, string name)
{
    private bool @void, returnsTask, wait;
    private ITypeSymbol returnType, taskResult;

    public void Emit (out string signature, out string body)
    {
        @void = method.ReturnsVoid;
        returnType = method.ReturnType;
        returnsTask = IsTaskWithResult(method.ReturnType, out var task);
        taskResult = task?.TypeArguments.FirstOrDefault();
        wait = returnsTask && ShouldSerialize(taskResult);
        signature = EmitSignature();
        body = EmitBody();
    }

    private string EmitSignature ()
    {
        var args = method.Parameters.Select(p => $"{BuildFullName(p.Type)} {p.Name}");
        var sig = $"{BuildFullName(method.ReturnType)} {name} ({string.Join(", ", args)})";
        if (wait) return sig = "async " + sig;
        return sig;
    }

    private string EmitBody ()
    {
        var endpoint = GetEndpoint(space);
        var delegateType = GetDelegateType();
        var args = GetArgs();
        var body = $"""Get<{delegateType}>("{endpoint}")({args})""";
        if (!ShouldSerialize(returnsTask ? taskResult : returnType)) return body;
        var serialized = BuildFullName(returnsTask ? taskResult : returnType);
        return $"Deserialize<{serialized}>({(wait ? "await " : "")}{body})";
    }

    private string GetDelegateType ()
    {
        if (@void && method.Parameters.Length == 0) return "global::System.Action";
        var name = @void ? "global::System.Action" : "global::System.Func";
        var args = method.Parameters.Select(GetDelegateArgType);
        if (!@void) args = args.Append(GetDelegateReturnType());
        return $"{name}<{string.Join(", ", args)}>";
    }

    private string GetDelegateArgType (IParameterSymbol param) =>
        ShouldSerialize(param.Type) ? "global::System.String" : BuildFullName(param.Type);

    private string GetDelegateReturnType () =>
        ShouldSerialize(returnsTask ? taskResult : returnType)
            ? (returnsTask ? "global::System.Threading.Tasks.Task<global::System.String>" : "global::System.String")
            : BuildFullName(returnType);

    private string GetEndpoint (string module)
    {
        name = ToFirstLower(name);
        if (@event) name += ".broadcast";
        return $"{module}.{name}";
    }

    private string GetArgs ()
    {
        if (method.Parameters.Length == 0) return "";
        var ids = method.Parameters.Select(GetArg);
        return string.Join(", ", ids);
    }

    private string GetArg (IParameterSymbol param)
    {
        return ShouldSerialize(param.Type) ? $"Serialize({param.Name})" : param.Name;
    }

    // see table at https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/import-export-interop
    public static bool ShouldSerialize (ITypeSymbol type)
    {
        var array = type is IArrayTypeSymbol;
        if (array) type = ((IArrayTypeSymbol)type).ElementType;
        if (IsNullable(type, out var nullable)) type = GetNullableUnderlyingType(nullable);
        if (array) return !IsArrayTransferable(type);
        return !IsStandaloneTransferable(type);
    }

    private static bool IsTaskWithResult (ITypeSymbol type, out INamedTypeSymbol named)
    {
        named = type as INamedTypeSymbol;
        return $"{type.ContainingNamespace}.{type.MetadataName}" == typeof(Task<>).FullName;
    }

    private static bool IsNullable (ITypeSymbol type, out INamedTypeSymbol nullable)
    {
        nullable = type as INamedTypeSymbol;
        return $"{type.ContainingNamespace}.{type.MetadataName}" == typeof(Nullable<>).FullName;
    }

    private static ITypeSymbol GetNullableUnderlyingType (INamedTypeSymbol task)
        => task?.TypeArguments.FirstOrDefault();

    private static bool IsStandaloneTransferable (ITypeSymbol type) =>
        Is<string>(type) || Is<bool>(type) || Is<byte>(type) || Is<char>(type) || Is<short>(type) ||
        Is<long>(type) || Is<int>(type) || Is<float>(type) || Is<double>(type) || Is<nint>(type) ||
        Is<DateTime>(type) || Is<DateTimeOffset>(type) || Is<Task>(type) || type.SpecialType == SpecialType.System_Void;

    private static bool IsArrayTransferable (ITypeSymbol type) =>
        Is<byte>(type) || Is<int>(type) || Is<double>(type) || Is<string>(type);

    private static bool Is<T> (ITypeSymbol type) =>
        $"{type.ContainingNamespace}.{type.MetadataName}" == typeof(T).FullName;
}
