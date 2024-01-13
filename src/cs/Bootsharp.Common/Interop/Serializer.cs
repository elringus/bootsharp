using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Bootsharp;

/// <summary>
/// Handles serialization of the interop values that can't be passed to and from JavaScript as-is.
/// </summary>
public static class Serializer
{
    /// <summary>
    /// Options for <see cref="JsonSerializer"/> used under the hood.
    /// </summary>
    public static JsonSerializerOptions Options { get; set; } = new(JsonSerializerDefaults.Web) {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes specified object to JSON string.
    /// </summary>
    public static string Serialize (object? @object)
    {
        if (@object is null) return "null";
        return JsonSerializer.Serialize(@object, GetInfo(@object.GetType()));
    }

    /// <summary>
    /// Deserializes specified JSON string to the object of specified type.
    /// </summary>
    public static T? Deserialize<T> (string? json)
    {
        if (json is null ||
            json.Equals("null", StringComparison.Ordinal) ||
            json.Equals("undefined", StringComparison.Ordinal)) return default;
        var info = (JsonTypeInfo<T>)GetInfo(typeof(T));
        return JsonSerializer.Deserialize(json, info);
    }

    private static JsonTypeInfo GetInfo (Type type)
    {
        if (Options.TypeInfoResolver is null)
            throw new Error("Serializer info resolver is not assigned.");
        return Options.TypeInfoResolver.GetTypeInfo(type, Options) ??
               throw new Error($"Failed to resolve serializer info for '{type}'.");
    }
}
