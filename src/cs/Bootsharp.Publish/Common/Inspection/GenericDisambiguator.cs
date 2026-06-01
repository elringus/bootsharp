using System.Reflection;

namespace Bootsharp.Publish;

/// <summary>
/// Rewrites <see cref="MethodMeta"/> associated with the generic methods expanding them into a concrete
/// overload for each user type compatible with the method's type parameter constraint.
/// </summary>
internal static class GenericDisambiguator
{
    public static void Disambiguate (TypeMeta[] types)
    {
        foreach (var surface in types.OfType<SurfaceMeta>())
        foreach (var method in surface.Members.OfType<MethodMeta>().ToArray())
            if (method.IK == InteropKind.Export && method.Info.ContainsGenericParameters)
                Disambiguate(surface, method, types);
    }

    private static void Disambiguate (SurfaceMeta surf, MethodMeta meth, TypeMeta[] types)
    {
        surf.MemberList.Remove(meth);
        foreach (var expanded in Expand(meth, types))
            surf.MemberList.Add(expanded);
    }

    private static IEnumerable<MethodMeta> Expand (MethodMeta meth, TypeMeta[] types)
    {
        if (!meth.Info.IsGenericMethodDefinition) yield break; // ignore methods declared on a generic type
        if (meth.Info.GetGenericArguments() is not [{ } param]) yield break; // only single <T> supported
        if (param.GetGenericParameterConstraints().FirstOrDefault(IsUserType) is not { } ct) yield break;
        if (HasNestedGeneric(meth.Info)) yield break; // type parameter nested in another type can't be rewritten
        foreach (var compatible in GetCompatible(ct, types))
            yield return CloseGeneric(meth, compatible);
    }

    private static bool HasNestedGeneric (MethodInfo meth) => meth
        .GetParameters().Select(p => p.ParameterType).Prepend(meth.ReturnType)
        .Any(t => t.ContainsGenericParameters && !t.IsGenericMethodParameter);

    private static IEnumerable<TypeMeta> GetCompatible (Type ct, TypeMeta[] types) => types
        .Where(t => t is InstanceMeta or SerializedObjectMeta && !t.Clr.IsAbstract && ct.IsAssignableFrom(t.Clr))
        .DistinctBy(t => t.Clr);

    private static MethodMeta CloseGeneric (MethodMeta meth, TypeMeta closeType)
    {
        var closed = meth.Info.MakeGenericMethod(closeType.Clr);
        return new MethodMeta(closed) {
            Surf = meth.Surf,
            IK = meth.IK,
            Name = $"{meth.Name}<{closeType.Syntax}>",
            Endpoint = $"{meth.Name}Of{closeType.Id}",
            JSName = $"{meth.JSName}Of{closeType.Clr.Name}",
            Args = meth.Args.Zip(closed.GetParameters(), RewriteArg).ToArray(),
            Return = meth.Info.ReturnType.IsGenericMethodParameter ? RewriteValue(meth.Return) : meth.Return,
            Void = meth.Void,
            Async = meth.Async
        };

        ArgumentMeta RewriteArg (ArgumentMeta arg, ParameterInfo param)
        {
            if (!arg.Info.ParameterType.IsGenericMethodParameter) return arg;
            var value = RewriteValue(arg.Value);
            return new ArgumentMeta(param) { Name = arg.Name, JSName = arg.JSName, Value = value };
        }

        ValueMeta RewriteValue (ValueMeta value) => value with {
            Type = closeType,
            TypeSyntax = value.Nullable ? $"{closeType.Syntax}?" : closeType.Syntax
        };
    }

    extension (SurfaceMeta srf)
    {
        private IList<MemberMeta> MemberList => (IList<MemberMeta>)srf.Members;
    }
}
