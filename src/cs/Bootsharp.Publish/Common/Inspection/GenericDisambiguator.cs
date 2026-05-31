using System.Reflection;

namespace Bootsharp.Publish;

/// <summary>
/// Rewrites <see cref="MethodMeta"/> associated with the generic methods expanding them into a concrete
/// overload for each user type compatible with the method's type parameter constraint.
/// </summary>
internal static class GenericDisambiguator
{
    private static IReadOnlyCollection<TypeMeta> types = null!;

    public static void Disambiguate (IReadOnlyCollection<TypeMeta> types)
    {
        GenericDisambiguator.types = types;
        foreach (var surface in types.OfType<SurfaceMeta>())
        foreach (var method in surface.Members.OfType<MethodMeta>().ToArray())
            if (method.IK == InteropKind.Export && method.Info.ContainsGenericParameters)
                Disambiguate(surface, method);
    }

    private static void Disambiguate (SurfaceMeta surf, MethodMeta meth)
    {
        surf.MemberList.Remove(meth);
        foreach (var expanded in Expand(meth))
            surf.MemberList.Add(expanded);
    }

    private static IEnumerable<MethodMeta> Expand (MethodMeta meth)
    {
        if (!meth.Info.IsGenericMethodDefinition) yield break; // ignore methods declared on a generic type
        if (meth.Info.GetGenericArguments() is not [{ } param]) yield break; // only single <T> supported
        if (param.GetGenericParameterConstraints().FirstOrDefault(IsUserType) is not { } ct) yield break;
        foreach (var compatible in GetCompatible(ct))
            yield return CloseGeneric(meth, compatible);
    }

    private static IEnumerable<TypeMeta> GetCompatible (Type constraint) => types
        .Where(t => t is InstanceMeta && !t.Clr.IsAbstract && constraint.IsAssignableFrom(t.Clr))
        .DistinctBy(t => t.Clr);

    private static MethodMeta CloseGeneric (MethodMeta meth, TypeMeta closeType)
    {
        var closed = meth.Info.MakeGenericMethod(closeType.Clr);
        return new MethodMeta(closed) {
            Surf = meth.Surf,
            IK = meth.IK,
            Name = $"{meth.Name}<{closeType.Syntax}>",
            Endpoint = $"{meth.Name}{closeType.Id}",
            JSName = $"{meth.JSName}With{closeType.Clr.Name}",
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
