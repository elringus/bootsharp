using System.Text.Json;

namespace Bootsharp;

/// <summary>
/// Handles serialization of the marshalled interop data.
/// </summary>
public static class Serializer
{
    /// <summary>
    /// Options for <see cref="JsonSerializer"/> used under the hood.
    /// </summary>
    public static JsonSerializerOptions Options { get; set; } = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Attempt to serialize specified object to JSON string.
    /// </summary>
    public static string Serialize (object @object) => JsonSerializer.Serialize(@object, Options);

    /// <summary>
    /// Attempt to deserialize specified JSON string to the object of specified type.
    /// </summary>
    public static object Deserialize (string json, Type type) => JsonSerializer.Deserialize(json, type, Options)!;
}
