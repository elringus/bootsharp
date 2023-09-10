using System.Text.Json;

namespace Bootsharp;

/// <summary>
/// Handles serialization of the interop data that can't be passed to and from JavaScript as-is.
/// </summary>
public static class Serializer
{
    /// <summary>
    /// Options for <see cref="JsonSerializer"/> used under the hood.
    /// </summary>
    public static JsonSerializerOptions Options { get; set; } = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Serializes specified object to JSON string.
    /// </summary>
    public static string Serialize (object @object) => JsonSerializer.Serialize(@object, Options);

    /// <summary>
    /// Deserializes specified JSON string to the object of specified type.
    /// </summary>
    public static T Deserialize<T> (string json) => JsonSerializer.Deserialize<T>(json, Options)!;
}
