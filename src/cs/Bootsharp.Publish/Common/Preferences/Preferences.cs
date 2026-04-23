namespace Bootsharp.Publish;

/// <inheritdoc cref="PreferencesAttribute"/>
internal sealed record Preferences
{
    /// <inheritdoc cref="PreferencesAttribute.Space"/>
    public IReadOnlyList<Preference> Space { get; init; } = [];
    /// <inheritdoc cref="PreferencesAttribute.Type"/>
    public IReadOnlyList<Preference> Type { get; init; } = [];
    /// <inheritdoc cref="PreferencesAttribute.Function"/>
    public IReadOnlyList<Preference> Function { get; init; } = [];
}
