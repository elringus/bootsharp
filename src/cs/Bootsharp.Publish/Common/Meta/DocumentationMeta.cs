using System.Xml.Linq;

namespace Bootsharp.Publish;

/// <summary>
/// C# XML documentation generated for an inspected assembly.
/// </summary>
/// <param name="Assembly">Name of the assembly associated with the documentation.</param>
/// <param name="Xml">The XML documentation.</param>
internal sealed record DocumentationMeta (string Assembly, XDocument Xml);
