using System.Reflection;

namespace Bootsharp.Publish;

/// <summary>
/// An interop member declared on a static API surface or interop interface.
/// </summary>
internal abstract record MemberMeta
{
    /// <summary>
    /// The reflected info of the member.
    /// </summary>
    public abstract MemberInfo Info { get; }
    /// <summary>
    /// Whether the member is implemented in C# and exposed to JavaScript (export)
    /// or implemented in JavaScript and consumed from C# (import).
    /// </summary>
    public required InteropKind Interop { get; init; }
    /// <summary>
    /// C# assembly name (DLL file name, w/o the extension), under which the member is declared.
    /// </summary>
    public required string Assembly { get; init; }
    /// <summary>
    /// Full name of the C# type (including namespace), under which the member is declared.
    /// </summary>
    public required string Space { get; init; }
    /// <summary>
    /// JavaScript object name(s) (joined with dot when nested) under which the associated interop
    /// member will be declared; resolved from <see cref="Space"/> with user-defined converters.
    /// </summary>
    public required string JSSpace { get; init; }
    /// <summary>
    /// C# name of the member, as specified in source code or generated for the interop implementation.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// JavaScript name of the member as will be specified in source code.
    /// </summary>
    public required string JSName { get; init; }
    /// <summary>
    /// Metadata of the value carried by the member.
    /// </summary>
    public required ValueMeta Value { get; init; }
}

/// <summary>
/// An interop method declared on a static API surface or interop interface.
/// </summary>
/// <remarks>
/// Return value of the method is described in <see cref="MemberMeta.Value"/>.
/// </remarks>
internal record MethodMeta (MethodInfo Info) : MemberMeta
{
    /// <summary>
    /// The reflected info of the method.
    /// </summary>
    public override MethodInfo Info { get; } = Info;
    /// <summary>
    /// Arguments of the method.
    /// </summary>
    public required IReadOnlyList<ArgumentMeta> Arguments { get; init; }
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
/// An interop event declared on a static API surface or interop interface.
/// </summary>
internal sealed record EventMeta (EventInfo Info) : MemberMeta
{
    /// <summary>
    /// The reflected info of the event.
    /// </summary>
    public override EventInfo Info { get; } = Info;
    /// <summary>
    /// Arguments carried by the event delegate.
    /// </summary>
    public required IReadOnlyList<ArgumentMeta> Arguments { get; init; }
}

/// <summary>
/// An interop property declared on an interop interface.
/// </summary>
internal sealed record PropertyMeta (PropertyInfo Info) : MemberMeta
{
    /// <summary>
    /// The reflected info of the property.
    /// </summary>
    public override PropertyInfo Info { get; } = Info;
    /// <summary>
    /// Whether the property has an accessible getter.
    /// </summary>
    public required bool CanGet { get; init; }
    /// <summary>
    /// Whether the property has an accessible setter.
    /// </summary>
    public required bool CanSet { get; init; }
}

/// <summary>
/// An interop method or event delegate argument.
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
