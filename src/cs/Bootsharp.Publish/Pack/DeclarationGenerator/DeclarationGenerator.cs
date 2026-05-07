namespace Bootsharp.Publish;

internal sealed class DeclarationGenerator (Preferences prefs)
{
    private readonly ModuleDeclarationGenerator modules = new(prefs);
    private readonly TypeDeclarationGenerator types = new(prefs);

    public string Generate (SolutionInspection spec) => Fmt(0,
        """import type { EventBroadcaster, EventSubscriber } from "./event";""",
        types.Generate(spec),
        modules.Generate(spec)
    ) + "\n";
}
