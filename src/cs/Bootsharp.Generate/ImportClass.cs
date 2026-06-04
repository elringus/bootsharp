namespace Bootsharp.Generate;

/// <summary>
/// A class hosting one or more imported members, grouped from the resolved members.
/// Emits a single partial declaration implementing all the bindings of that class.
/// </summary>
internal sealed record ImportClass (string Space, string Name, string TypeParams, string Modifiers, string Members)
{
    public string Emit ()
    {
        var mods = Modifiers.Contains("unsafe") ? Modifiers : Modifiers.Replace("partial", "unsafe partial");
        var code = string.Join("\n", Members.Split('\n').Select(static line => $"    {line}"));
        var space = Space.Length == 0 ? "" : $"namespace {Space};\n\n";
        return $"#nullable enable\n#pragma warning disable\n\n" +
               $"{space}{mods} class {Name}{TypeParams}\n{{\n{code}\n}}";
    }

    public static IEnumerable<ImportClass> Group (IEnumerable<ImportMember> members) => members
        .GroupBy(static member => (member.Space, member.Class, member.TypeParams))
        .Select(static byClass => new ImportClass(
            Space: byClass.Key.Space,
            Name: byClass.Key.Class,
            TypeParams: byClass.Key.TypeParams,
            Modifiers: byClass.First().Modifiers,
            Members: string.Join("\n", byClass.Select(static m => m.Code))));
}
