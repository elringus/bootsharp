namespace Bootsharp.Publish;

internal sealed record Preferences
{
    public IReadOnlyList<Preference> Space { get; init; } = [];
    public IReadOnlyList<Preference> Type { get; init; } = [];
    public IReadOnlyList<Preference> Event { get; init; } = [];
    public IReadOnlyList<Preference> Function { get; init; } = [];
}
