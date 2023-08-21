using System;

namespace Bootsharp;

/// <summary>
/// The attribute for automatic JS bindings generation of specified types.
/// </summary>
public abstract class JSTypeAttribute : Attribute
{
    /// <summary>
    /// The types to generated bindings for.
    /// </summary>
    public Type[] Types { get; }
    /// <summary>
    /// Regex pattern to match generated method names.
    /// </summary>
    public string? NamePattern { get; init; }
    /// <summary>
    /// Replacement for the pattern matches of the generated method names.
    /// </summary>
    public string? NameReplacement { get; init; }
    /// <summary>
    /// Regex pattern to match generated method invocations.
    /// </summary>
    public string? InvokePattern { get; init; }
    /// <summary>
    /// Replacement for the pattern matches of the generated method invocations.
    /// </summary>
    public string? InvokeReplacement { get; init; }

    /// <param name="types">The types to generated bindings for.</param>
    protected JSTypeAttribute (Type[] types)
    {
        Types = types;
    }
}
