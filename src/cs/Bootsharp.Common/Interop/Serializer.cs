using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Bootsharp;

/// <summary>
/// Handles serialization of the interop values that can't be passed to and from JavaScript as-is.
/// </summary>
public static class Serializer
{
    /// <summary>
    /// Serializes specified object to JSON string using specified serialization context info.
    /// </summary>
    public static string Serialize<T> (T? @object, JsonTypeInfo<T> info)
    {
        if (@object is null) return "null";
        return JsonSerializer.Serialize(@object, info);
    }

    /// <summary>
    /// Deserializes specified JSON string to the object of specified type.
    /// </summary>
    public static T? Deserialize<T> (string? json, JsonTypeInfo<T> info)
    {
        if (json is null ||
            json.Equals("null", StringComparison.Ordinal) ||
            json.Equals("undefined", StringComparison.Ordinal)) return default;
        return JsonSerializer.Deserialize(json, info);
    }
}
