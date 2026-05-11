using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bootsharp.Publish;

/// <summary>
/// Describes a member declared on an interop surface (<see cref="SurfaceMeta"/>).
/// </summary>
internal abstract record MemberMeta
{
    /// <summary>
    /// The reflected info of the member.
    /// </summary>
    public abstract MemberInfo Info { get; }
    /// <summary>
    /// Describes the interop surface on which the member is declared.
    /// </summary>
    public required SurfaceMeta Surf { get; init; }
    /// <summary>
    /// Whether the member is implemented in C# and exposed to JavaScript (export)
    /// or implemented in JavaScript and consumed from C# (import).
    /// </summary>
    public required InteropKind IK { get; init; }
    /// <summary>
    /// C# name of the member, as specified in source code or generated for the interop implementation.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// JavaScript name of the member as will be specified in source code.
    /// </summary>
    public required string JSName { get; init; }
}

/// <summary>
/// Describes a method declared on an interop surface.
/// </summary>
internal record MethodMeta (MethodInfo Info) : MemberMeta
{
    /// <summary>
    /// The reflected info of the method.
    /// </summary>
    public override MethodInfo Info { get; } = Info;
    /// <summary>
    /// Arguments of the method.
    /// </summary>
    public required IReadOnlyList<ArgumentMeta> Args { get; init; }
    /// <summary>
    /// Method's return value.
    /// </summary>
    public required ValueMeta Return { get; init; }
    /// <summary>
    /// Whether the method returns void.
    /// </summary>
    public required bool Void { get; init; }
    /// <summary>
    /// Whether the method returns is task-like value (can be awaited).
    /// </summary>
    public required bool Async { get; init; }
}

/// <summary>
/// Describes an event declared on an interop surface.
/// </summary>
internal sealed record EventMeta (EventInfo Info) : MemberMeta
{
    /// <summary>
    /// The reflected info of the event.
    /// </summary>
    public override EventInfo Info { get; } = Info;
    /// <summary>
    /// Fully qualified C# syntax of the event type, including nullable annotations.
    /// </summary>
    public required string TypeSyntax { get; init; }
    /// <summary>
    /// Arguments carried by the event delegate.
    /// </summary>
    public required IReadOnlyList<ArgumentMeta> Args { get; init; }
}

/// <summary>
/// Describes a property declared on an interop surface.
/// </summary>
internal sealed record PropertyMeta (PropertyInfo Info) : MemberMeta
{
    /// <summary>
    /// The reflected info of the property.
    /// </summary>
    public override PropertyInfo Info { get; } = Info;
    /// <summary>
    /// Fully qualified C# syntax of the property type, including nullable annotations.
    /// </summary>
    public required string TypeSyntax { get; init; }
    /// <summary>
    /// Describes the getter value of the property.
    /// </summary>
    public required ValueMeta? Get { get; init; }
    /// <summary>
    /// Describes the setter value of the property.
    /// </summary>
    public required ValueMeta? Set { get; init; }
    /// <summary>
    /// Whether the property has an accessible getter.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Get))]
    public bool CanGet => Get != null;
    /// <summary>
    /// Whether the property has an accessible setter.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Set))]
    public bool CanSet => Set != null;
}

/// <summary>
/// Describes a method or event delegate argument.
/// </summary>
internal sealed record ArgumentMeta (ParameterInfo Info)
{
    /// <summary>
    /// The reflected info of the argument.
    /// </summary>
    public ParameterInfo Info { get; } = Info;
    /// <summary>
    /// C# name of the argument, as specified in source code.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// JavaScript name of the argument, to be specified in source code.
    /// </summary>
    public required string JSName { get; init; }
    /// <summary>
    /// Metadata of the argument's value.
    /// </summary>
    public required ValueMeta Value { get; init; }
}
