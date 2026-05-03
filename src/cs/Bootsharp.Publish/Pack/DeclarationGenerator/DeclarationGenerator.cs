namespace Bootsharp.Publish;

internal sealed class DeclarationGenerator (Preferences prefs)
{
    private readonly MemberDeclarationGenerator members = new(prefs);
    private readonly TypeDeclarationGenerator types = new(prefs);

    public string Generate (SolutionInspection spec) => Fmt(0,
        """import type { EventBroadcaster, EventSubscriber } from "./event";""",
        types.Generate(spec),
        members.Generate(spec)
    ) + "\n";
}
