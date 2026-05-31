namespace Bootsharp;

/// <summary>
/// When applied to a static method, designates it as the customizer of JavaScript node names —
/// the object that represents a CLR type under its module.
/// </summary>
/// <remarks>
/// The annotated method has to be static and accept the inspected CLR <see cref="System.Type"/> together
/// with the default node name (the reflected C# type name), returning the desired node name. It is invoked
/// for every inspected type after the metadata is collected. Return the supplied default to keep the node
/// unchanged; returning an empty, null or whitespace string erases the type, omitting it from the generated
/// JavaScript.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RenameNodeAttribute : Attribute;
