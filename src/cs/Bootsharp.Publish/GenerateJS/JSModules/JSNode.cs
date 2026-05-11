namespace Bootsharp.Publish;

/// <summary>
/// A JavaScript node declared under a <see cref="JSModule"/>.
/// </summary>
/// <remarks>
/// Nodes are standalone JavaScript or TypeScript artifacts declared under a module,
/// and include objects, enums and namespaces.
/// </remarks>
internal sealed record JSNode
{
    /// <summary>
    /// Name of the node.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// Types represented by the node.
    /// </summary>
    /// <remarks>
    /// In most cases types are mapped 1:1 to JS nodes, with the following exceptions:
    /// 1. Two sides of a bidirectional (import+export) instance surface, both end up in the same node.
    /// 2. User prefs collapsing unique type names into one, all end in the same node.
    /// </remarks>
    public required IReadOnlyList<TypeMeta> Types { get; init; }
    /// <summary>
    /// The node's children.
    /// </summary>
    public required IReadOnlyList<JSNode> Children { get; init; }

    /// <summary>
    /// Whether any type under the node or any of the child nodes satisfies the predicate.
    /// </summary>
    public bool Any (Predicate<TypeMeta> filter)
    {
        foreach (var type in Types)
            if (filter(type))
                return true;
        foreach (var node in Children)
            if (node.Any(filter))
                return true;
        return false;
    }
}
