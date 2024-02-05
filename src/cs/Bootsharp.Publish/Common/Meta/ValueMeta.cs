using System.Diagnostics.CodeAnalysis;

namespace Bootsharp.Publish;

/// <summary>
/// Interop method's argument or returned value.
/// </summary>
internal sealed record ValueMeta
{
    /// <summary>
    /// C# type of the value.
    /// </summary>
    public required Type Type { get; init; }
    /// <summary>
    /// C# syntax of the value type, as specified in source code.
    /// </summary>
    public required string TypeSyntax { get; init; }
    /// <summary>
    /// TypeScript syntax of the value type, to be specified in source code.
    /// </summary>
    public required string JSTypeSyntax { get; init; }
    /// <summary>
    /// Whether the value is optional/nullable.
    /// </summary>
    public required bool Nullable { get; init; }
    /// <summary>
    /// Whether the value type is of an async nature (eg, task or promise).
    /// </summary>
    public required bool Async { get; init; }
    /// <summary>
    /// Whether the value is void (when method return value).
    /// </summary>
    public required bool Void { get; init; }
    /// <summary>
    /// Whether the value has to be marshalled to/from JSON for interop.
    /// </summary>
    public required bool Serialized { get; init; }
    /// <summary>
    /// Whether the value is an interop instance.
    /// </summary>
    [MemberNotNullWhen(true, nameof(InstanceType))]
    public required bool Instance { get; init; }
    /// <summary>
    /// When <see cref="Instance"/> contains type of the associated interop interface instance.
    /// </summary>
    public required Type? InstanceType { get; init; }
}
