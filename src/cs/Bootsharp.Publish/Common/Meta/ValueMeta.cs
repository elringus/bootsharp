using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bootsharp.Publish;

/// <summary>
/// Describes a value that crosses the interop boundary.
/// </summary>
internal sealed record ValueMeta
{
    /// <summary>
    /// Type info of the value.
    /// </summary>
    public required TypeMeta Type { get; init; }
    /// <summary>
    /// Fully qualified C# syntax of the value type.
    /// In contrast to <see cref="TypeMeta.Syntax"/> includes nullable annotation of the value, if any.
    /// </summary>
    public required string TypeSyntax { get; init; }
    /// <summary>
    /// Whether the value is explicitly nullable: has nullable annotation or is <see cref="System.Nullable"/>.
    /// </summary>
    public required bool Nullable { get; init; }
    /// <summary>
    /// Nullability context of the value.
    /// </summary>
    public required NullabilityInfo Nullability { get; init; }
    /// <summary>
    /// Serialization info when <see cref="IsSerialized"/>, null otherwise.
    /// </summary>
    public required SerializedMeta? Serialized { get; init; }
    /// <summary>
    /// Instance info when <see cref="IsInstanced"/>, null otherwise.
    /// </summary>
    public required InstancedMeta? Instanced { get; init; }
    /// <summary>
    /// Whether the value has to be serialized to cross the interop boundary.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Serialized))]
    public bool IsSerialized => Serialized != null;
    /// <summary>
    /// Whether the value is an interop instance.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Instanced))]
    public bool IsInstanced => Instanced != null;
}
