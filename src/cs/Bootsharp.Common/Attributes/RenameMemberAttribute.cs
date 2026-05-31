namespace Bootsharp;

/// <summary>
/// When applied to a static method, designates it as the customizer of JavaScript member names —
/// the methods, properties and events projected on an interop surface.
/// </summary>
/// <remarks>
/// The annotated method has to be static and accept the reflected <see cref="System.Reflection.MemberInfo"/>
/// together with the default member name (camel-cased and disambiguated), returning the desired member name.
/// It is invoked for every inspected member after the metadata is collected. Return the supplied default to
/// keep the member unchanged; returning an empty, null or whitespace string erases the member, omitting it
/// from the generated JavaScript.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RenameMemberAttribute : Attribute;
