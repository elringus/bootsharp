using System.Diagnostics.CodeAnalysis;

namespace Bootsharp;

/// <summary>
/// User preferences for Bootsharp behaviour. Inherit, override required methods and
/// supply inherited class to <see cref="JSConfigurationAttribute{T}"/>.
/// </summary>
[SuppressMessage("ReSharper", "UnusedParameter.Global",
    Justification = "Accessed in Bootsharp.Publish.Test via generated code.")]
public class Preferences
{
    /// <summary>
    /// Builds JavaScript namespace (object chain) for specified C# type.
    /// </summary>
    /// <remarks>
    /// This affect both objects generated to host bindings and type names
    /// of the values referenced in the bindings. When building binding host
    /// object name, the <paramref name="type"/> is declaring type of the
    /// associated method.
    /// </remarks>
    /// <param name="type">C# type to build namespace for.</param>
    /// <param name="default">Result when processed w/o the override.</param>
    public virtual string BuildSpace (Type type, string @default) => @default;
}
