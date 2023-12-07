using Microsoft.CodeAnalysis;

namespace Bootsharp.Generator;

internal class BindingEmitter (IMethodSymbol method, string space, string name)
{
    private bool @void, wait, shouldSerializeReturnType;
    private ITypeSymbol returnType, taskResult;

    public void Emit (out string signature, out string body)
    {
        @void = method.ReturnsVoid;
        returnType = method.ReturnType;
        IsTaskWithResult(method.ReturnType, out taskResult);
        shouldSerializeReturnType = ShouldSerialize(returnType);
        wait = taskResult != null && shouldSerializeReturnType;
        signature = EmitSignature();
        body = EmitBody();
    }

    private string EmitSignature ()
    {
        var args = method.Parameters.Select(p => $"{BuildSyntax(p.Type)} {p.Name}");
        var sig = $"{BuildSyntax(method.ReturnType)} {name} ({string.Join(", ", args)})";
        if (wait) return sig = "async " + sig;
        return sig;
    }

    private string EmitBody ()
    {
        var endpoint = $"{space}.{ToFirstLower(name)}";
        var delegateType = GetDelegateType();
        var args = GetArgs();
        var body = $"""Get<{delegateType}>("{endpoint}")({args})""";
        if (!shouldSerializeReturnType) return body;
        var serialized = BuildSyntax(taskResult ?? returnType);
        return $"Deserialize<{serialized}>({(wait ? "await " : "")}{body})";
    }

    private string GetDelegateType ()
    {
        if (@void && method.Parameters.Length == 0) return "global::System.Action";
        var basename = @void ? "global::System.Action" : "global::System.Func";
        var args = method.Parameters.Select(GetDelegateArgType);
        if (!@void) args = args.Append(GetDelegateReturnType());
        return $"{basename}<{string.Join(", ", args)}>";
    }

    private string GetDelegateArgType (IParameterSymbol param) =>
        ShouldSerialize(param.Type) ? "global::System.String" : BuildSyntax(param.Type);

    private string GetDelegateReturnType () => shouldSerializeReturnType
        ? (taskResult != null ? "global::System.Threading.Tasks.Task<global::System.String>" : "global::System.String")
        : BuildSyntax(returnType);

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

    // https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/import-export-interop
    private static bool ShouldSerialize (ITypeSymbol type)
    {
        if (IsTaskWithResult(type, out var taskResult)) type = taskResult;
        var array = type is IArrayTypeSymbol;
        if (array) type = ((IArrayTypeSymbol)type).ElementType;
        if (IsNullable(type, out var nullable)) type = nullable.TypeArguments.FirstOrDefault();
        if (array) return taskResult != null || !IsArrayTransferable(type);
        return !IsStandaloneTransferable(type);

        static bool IsNullable (ITypeSymbol type, out INamedTypeSymbol nullable)
        {
            nullable = type as INamedTypeSymbol;
            return $"{type.ContainingNamespace}.{type.MetadataName}" == typeof(Nullable<>).FullName;
        }

        static bool IsStandaloneTransferable (ITypeSymbol type) =>
            Is<string>(type) || Is<bool>(type) || Is<byte>(type) || Is<char>(type) || Is<short>(type) ||
            Is<long>(type) || Is<int>(type) || Is<float>(type) || Is<double>(type) || Is<nint>(type) ||
            Is<Exception>(type) || Is<DateTime>(type) || Is<DateTimeOffset>(type) || Is<Task>(type) ||
            type.SpecialType == SpecialType.System_Void;

        static bool IsArrayTransferable (ITypeSymbol type) =>
            Is<byte>(type) || Is<int>(type) || Is<double>(type) || Is<string>(type);

        static bool Is<T> (ITypeSymbol type) =>
            $"{type.ContainingNamespace}.{type.MetadataName}" == typeof(T).FullName;
    }

    private static bool IsTaskWithResult (ITypeSymbol type, out ITypeSymbol result)
    {
        return (result = $"{type.ContainingNamespace}.{type.MetadataName}" == typeof(Task<>).FullName
            ? ((INamedTypeSymbol)type).TypeArguments[0] : null) != null;
    }
}
