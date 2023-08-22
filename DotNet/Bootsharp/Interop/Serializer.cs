using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;

namespace Bootsharp;

/// <summary>
/// Handles JSON serialization of method and function arguments and return values.
/// </summary>
public class Serializer
{
    private readonly JsonSerializerOptions options;
    private readonly Dictionary<MethodInfo, ParameterInfo[]> methodToParams = new();

    /// <summary>
    /// Creates new serializer instanced with specified options.
    /// </summary>
    public Serializer (JsonSerializerOptions? options = null)
    {
        this.options = options ?? JsonSerializerOptions.Default;
    }

    /// <summary>
    /// Attempt to serialize specified object to JSON string.
    /// </summary>
    public string Serialize (object @object) => JsonSerializer.Serialize(@object, options);

    /// <summary>
    /// Attempt to deserialize specified JSON string to the object of specified type.
    /// </summary>
    public object Deserialize (string json, Type type)
        => JsonSerializer.Deserialize(json, type, options) ??
           throw new SerializationException($"Failed to deserialize '{json}' JSON to '{type.FullName}'.");

    /// <summary>
    /// Attempts to deserialize arguments described by the specified parameters info.
    /// </summary>
    public object[] DeserializeArgs (IReadOnlyList<string> args, IReadOnlyList<ParameterInfo> @params)
    {
        if (args.Count > @params.Count)
            throw new FormatException($"Failed to deserialize '{string.Join(',', args)}' arguments: incorrect count.");
        var result = new object[@params.Count];
        for (int i = 0; i < args.Count; i++)
            result[i] = Deserialize(args[i], @params[i].ParameterType);
        return result;
    }
}
