using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bootsharp.Publish;

/// <summary>
/// An interop member declared on a static, module or instanced API surface.
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
}

/// <summary>
/// An interop method declared on a static, module or instanced API surface.
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
    public required IReadOnlyList<ArgumentMeta> Arguments { get; init; }
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
/// An interop event declared on a static, module or instanced API surface.
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
/// An interop property declared on a module or instanced API surface.
/// </summary>
internal sealed record PropertyMeta (PropertyInfo Info) : MemberMeta
{
    /// <summary>
    /// The reflected info of the property.
    /// </summary>
    public override PropertyInfo Info { get; } = Info;
    /// <summary>
    /// Get value of the property or null when getter is not accessible.
    /// </summary>
    public required ValueMeta? GetValue { get; init; }
    /// <summary>
    /// Set value of the property or null when setter is not accessible.
    /// </summary>
    public required ValueMeta? SetValue { get; init; }
    /// <summary>
    /// Whether the property has an accessible getter.
    /// </summary>
    [MemberNotNullWhen(true, nameof(GetValue))]
    public bool CanGet => GetValue != null;
    /// <summary>
    /// Whether the property has an accessible setter.
    /// </summary>
    [MemberNotNullWhen(true, nameof(SetValue))]
    public bool CanSet => SetValue != null;
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
