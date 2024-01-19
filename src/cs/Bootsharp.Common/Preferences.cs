using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bootsharp;

/// <summary>
/// User preferences for Bootsharp behaviour. Inherit, override methods and
/// supply inherited class type to <see cref="JSConfigurationAttribute{T}"/>.
/// </summary>
[SuppressMessage("ReSharper", "UnusedParameter.Global",
    Justification = "Accessed in Bootsharp.Publish.Test via generated code.")]
public class Preferences
{
    /// <summary>
    /// Resolves JavaScript namespace (object names chain) for specified C# type.
    /// </summary>
    /// <remarks>
    /// This affect both objects generated to host bindings and type names
    /// of the values referenced in the bindings. When building binding host
    /// object name, the <paramref name="type"/> is declaring type of the
    /// associated method.
    /// </remarks>
    /// <param name="type">C# type to resolve namespace from.</param>
    /// <param name="default">Result when resolved w/o the override.</param>
    public virtual string ResolveSpace (Type type, string @default) => @default;
    /// <summary>
    /// Resolves TypeScript type syntax of a function member or object property from associated C# type.
    /// </summary>
    /// <param name="type">C# type to resolve TypeScript syntax from.</param>
    /// <param name="nullability">C# nullability info, ie whether associated member is nullable.</param>
    /// <param name="default">Result when resolved w/o the override.</param>
    public virtual string ResolveType (Type type, NullabilityInfo? nullability, string @default) => @default;
    /// <summary>
    /// Resolves interop-specific metadata for an interop interface specified under
    /// <see cref="JSExportAttribute"/> or <see cref="JSImportAttribute"/>.
    /// </summary>
    /// <param name="type">The interface type.</param>
    /// <param name="kind">Whether the interface exports to or imports APIs from JavaScript.</param>
    /// <param name="default">Result when resolved w/o the override.</param>
    public virtual InterfaceMeta ResolveInterface (Type type, InterfaceKind kind, InterfaceMeta @default) => @default;
    /// <summary>
    /// Resolves interop-specific metadata for an interop method attributed with either
    /// <see cref="JSInvokableAttribute"/>, <see cref="JSFunctionAttribute"/> or <see cref="JSEventAttribute"/>.
    /// </summary>
    /// <param name="info">Info about the interop method.</param>
    /// <param name="kind">Whether the method is intended to be invoked in JavaScript or vice-versa.</param>
    /// <param name="default">Result when resolved w/o the override.</param>
    public virtual MethodMeta ResolveMethod (MethodInfo info, MethodKind kind, MethodMeta @default) => @default;
}
