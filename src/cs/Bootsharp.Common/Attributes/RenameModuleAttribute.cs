namespace Bootsharp;

/// <summary>
/// When applied to a static method, designates it as the customizer of JavaScript module names —
/// the namespace-derived path that groups generated bindings and declarations.
/// </summary>
/// <remarks>
/// The annotated method has to be static and accept the inspected CLR <see cref="System.Type"/> together
/// with the default module name (the slugified C# namespace, or 'index' for global types), returning the
/// desired module name. It is invoked for every inspected type after the metadata is collected. Return the
/// supplied default to keep the module unchanged; returning an empty, null or whitespace string falls back
/// to the default 'index' module.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RenameModuleAttribute : Attribute;
