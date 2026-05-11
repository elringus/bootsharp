namespace Bootsharp.Publish;

/// <inheritdoc cref="PreferencesAttribute"/>
internal sealed record Preferences
{
    /// <inheritdoc cref="PreferencesAttribute.Space"/>
    public IReadOnlyList<Preference> Space { get; init; } = [];
    /// <inheritdoc cref="PreferencesAttribute.Name"/>
    public IReadOnlyList<Preference> Name { get; init; } = [];
    /// <inheritdoc cref="PreferencesAttribute.Method"/>
    public IReadOnlyList<Preference> Method { get; init; } = [];
    /// <inheritdoc cref="PreferencesAttribute.Property"/>
    public IReadOnlyList<Preference> Property { get; init; } = [];
    /// <inheritdoc cref="PreferencesAttribute.Event"/>
    public IReadOnlyList<Preference> Event { get; init; } = [];
}
