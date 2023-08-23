using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

namespace Bootsharp;

/// <summary>
/// Handles JSON serialization of method and function arguments and return values.
/// </summary>
public static class Serializer
{
    /// <summary>
    /// Options for <see cref="JsonSerializer"/> used under the hood.
    /// </summary>
    public static JsonSerializerOptions Options { get; set; } = JsonSerializerOptions.Default;

    /// <summary>
    /// Attempt to serialize specified object to JSON string.
    /// </summary>
    public static string Serialize (object @object) => JsonSerializer.Serialize(@object, Options);

    /// <summary>
    /// Attempt to deserialize specified JSON string to the object of specified type.
    /// </summary>
    public static object Deserialize (string json, Type type) => JsonSerializer.Deserialize(json, type, Options)!;

    /// <summary>
    /// Attempts to serialize specified arguments; returns null when args array is empty.
    /// </summary>
    public static string[]? SerializeArgs (params object[] args)
    {
        if (args.Length == 0) return null;
        var serialized = new string[args.Length];
        for (int i = 0; i < args.Length; i++)
            serialized[i] = Serialize(args[i]);
        return serialized;
    }

    /// <summary>
    /// Attempts to deserialize arguments described by the specified parameters info.
    /// </summary>
    public static object[] DeserializeArgs (IReadOnlyList<ParameterInfo> @params, params string[] args)
    {
        if (args.Length > @params.Count)
            throw new Error($"Failed to deserialize '{string.Join(',', args)}' arguments: the method doesn't accept as many arguments.");
        var result = new object[@params.Count];
        for (int i = 0; i < args.Length; i++)
            result[i] = Deserialize(args[i], @params[i].ParameterType);
        return result;
    }
}
