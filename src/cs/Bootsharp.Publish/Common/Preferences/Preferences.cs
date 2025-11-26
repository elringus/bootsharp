namespace Bootsharp.Publish;

/// <inheritdoc cref="JSPreferencesAttribute"/>
internal sealed record Preferences
{
    /// <inheritdoc cref="JSPreferencesAttribute.Space"/>
    public IReadOnlyList<Preference> Space { get; init; } = [];
    /// <inheritdoc cref="JSPreferencesAttribute.Type"/>
    public IReadOnlyList<Preference> Type { get; init; } = [];
    /// <inheritdoc cref="JSPreferencesAttribute.Event"/>
    public IReadOnlyList<Preference> Event { get; init; } = [];
    /// <inheritdoc cref="JSPreferencesAttribute.Function"/>
    public IReadOnlyList<Preference> Function { get; init; } = [];
}
