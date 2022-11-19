using System;

namespace DotNetJS;

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
    public string? NamePattern { get; }
    /// <summary>
    /// Replacement for the pattern matches of the generated method names.
    /// </summary>
    public string? NameReplacement { get; }
    /// <summary>
    /// Regex pattern to match generated method invocations.
    /// </summary>
    public string? InvokePattern { get; }
    /// <summary>
    /// Replacement for the pattern matches of the generated method invocations.
    /// </summary>
    public string? InvokeReplacement { get; }

    /// <param name="types">The types to generated bindings for.</param>
    /// <param name="namePattern">Regex pattern to match generated method names.</param>
    /// <param name="nameReplacement">Replacement for the pattern matches of the generated method names.</param>
    /// <param name="invokePattern">Regex pattern to match generated method invocations.</param>
    /// <param name="invokeReplacement">Replacement for the pattern matches of the generated method invocations.</param>
    protected JSTypeAttribute (Type[] types,
        string? namePattern = null, string? nameReplacement = null,
        string? invokePattern = null, string? invokeReplacement = null)
    {
        Types = types;
        NamePattern = namePattern;
        NameReplacement = nameReplacement;
        InvokePattern = invokePattern;
        InvokeReplacement = invokeReplacement;
    }
}
