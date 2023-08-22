using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

namespace Bootsharp;

/// <summary>
/// Handles JSON serialization of method and function arguments and return values.
/// </summary>
public sealed class Serializer
{
    /// <summary>
    /// Options for <see cref="JsonSerializer"/> used under the hood.
    /// </summary>
    public static JsonSerializerOptions Options { get; set; } = JsonSerializerOptions.Default;

    private readonly Dictionary<MethodInfo, ParameterInfo[]> methodToParams = new();

    /// <summary>
    /// Attempt to serialize specified object to JSON string.
    /// </summary>
    public string Serialize (object @object) => JsonSerializer.Serialize(@object, Options);

    /// <summary>
    /// Attempt to deserialize specified JSON string to the object of specified type.
    /// </summary>
    public object Deserialize (string json, Type type) => JsonSerializer.Deserialize(json, type, Options)!;

    /// <summary>
    /// Attempts to deserialize arguments described by the specified parameters info.
    /// </summary>
    public object[] DeserializeArgs (IReadOnlyList<string> args, IReadOnlyList<ParameterInfo> @params)
    {
        if (args.Count > @params.Count)
            throw new Error($"Failed to deserialize '{string.Join(',', args)}' arguments: the method doesn't accept as many arguments.");
        var result = new object[@params.Count];
        for (int i = 0; i < args.Count; i++)
            result[i] = Deserialize(args[i], @params[i].ParameterType);
        return result;
    }
}
