namespace Bootsharp.Publish;

/// <summary>
/// Renames <see cref="MethodMeta"/> associated with the overloaded methods making the names unique.
/// </summary>
internal static class OverloadDisambiguator
{
    public static void Disambiguate (TypeMeta[] types)
    {
        foreach (var surface in types.OfType<SurfaceMeta>())
        foreach (var overloaded in surface.Members.OfType<MethodMeta>()
                     .GroupBy(m => m.JSName).Where(g => g.Count() > 1))
            Disambiguate(surface, overloaded.ToArray());
    }

    private static void Disambiguate (SurfaceMeta srf, IReadOnlyList<MethodMeta> overloaded)
    {
        var argsByOverload = MapArgs(overloaded); // attempt to disambiguate only with extra arg names
        foreach (var method in FindAmbiguous(argsByOverload)) // if ambiguous, try with all arg names
            argsByOverload[method] = GetArgNames(method);
        foreach (var method in FindAmbiguous(argsByOverload)) // if still ambiguous, use arg type names
            argsByOverload[method] = GetArgTypes(method);
        foreach (var (method, args) in argsByOverload)
            Rename(srf, method, string.Join("And", args));
    }

    private static Dictionary<MethodMeta, string[]> MapArgs (IReadOnlyList<MethodMeta> overloaded)
    {
        var baseline = ResolveBaseline(overloaded); // baseline is the method that won't be renamed
        var baselineArgs = baseline.Args.Select(a => a.JSName).ToHashSet();
        return overloaded.Where(m => m != baseline).ToDictionary(m => m, m => m.Args
            .Where(a => !baselineArgs.Contains(a.JSName))
            .Select(a => ToFirstUpper(a.Info.Name!))
            // if an overload has extra args — use their names as discriminator
            .ToArray() is { Length: > 0 } extra ? extra : GetArgNames(m));
    }

    private static IEnumerable<MethodMeta> FindAmbiguous (Dictionary<MethodMeta, string[]> args) => args
        .GroupBy(kv => string.Join("|", kv.Value))
        .Where(g => g.Count() > 1)
        .SelectMany(g => g.Select(kv => kv.Key));

    private static string[] GetArgNames (MethodMeta method) => method.Args
        .Select(a => ToFirstUpper(a.Info.Name!)).ToArray();

    private static string[] GetArgTypes (MethodMeta method) => method.Args
        .Select(a => a.Value.Type.Clr.Name).ToArray();

    private static MethodMeta ResolveBaseline (IReadOnlyList<MethodMeta> overloaded) => overloaded
        .OrderBy(m => m.Args.Count)
        .ThenBy(m => string.Join("|", m.Args.Select(a => a.Value.Type.Clr.FullName)))
        .First();

    private static void Rename (SurfaceMeta srf, MethodMeta meth, string discriminator)
    {
        var members = (IList<MemberMeta>)srf.Members;
        members[members.IndexOf(meth)] = meth with {
            JSName = $"{meth.JSName}With{discriminator}",
            Endpoint = $"{meth.Endpoint}With{discriminator}"
        };
    }
}
