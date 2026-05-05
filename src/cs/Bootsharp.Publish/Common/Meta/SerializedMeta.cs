namespace Bootsharp.Publish;

/// <summary>
/// Describes an immutable CLR type that is serialized and copied by value when crossing the interop boundary.
/// </summary>
internal abstract record SerializedMeta (Type Clr) : TypeMeta(Clr)
{
    /// <summary>
    /// The identifier of the serializer factory associated with the type.
    /// </summary>
    public string Id { get; } = BuildSerializedId(Clr);
}

/// <summary>
/// Describes a serialized primitive (string, int, bool, etc).
/// </summary>
internal sealed record SerializedPrimitiveMeta (Type Clr) : SerializedMeta(Clr);

/// <summary>
/// Describes a serialized <see cref="System.Enum"/>.
/// </summary>
internal sealed record SerializedEnumMeta (Type Clr) : SerializedMeta(Clr);

/// <summary>
/// Describes a serialized <see cref="System.Nullable"/>.
/// </summary>
/// <param name="Value">Describes a serialized <see cref="System.Nullable"/>.</param>
internal sealed record SerializedNullableMeta (Type Clr, SerializedMeta Value) : SerializedMeta(Clr);

/// <summary>
/// Describes a serialized <see cref="System.Array"/>.
/// </summary>
/// <param name="Element">The array element.</param>
internal sealed record SerializedArrayMeta (Type Clr, SerializedMeta Element) : SerializedMeta(Clr);

/// <summary>
/// Describes a serialized linear collection type, such as generic lists and single-argument generic collections.
/// </summary>
/// <param name="Element">The collection element.</param>
internal sealed record SerializedListMeta (Type Clr, SerializedMeta Element) : SerializedMeta(Clr);

/// <summary>
/// Describes a serialized generic key-value type, such as generic dictionaries.
/// </summary>
/// <param name="Key">The dictionary key.</param>
/// <param name="Value">The dictionary value.</param>
internal sealed record SerializedDictionaryMeta (Type Clr,
    SerializedMeta Key, SerializedMeta Value) : SerializedMeta(Clr);

/// <summary>
/// Describes a serialized user-defined object, such as a record or a struct.
/// </summary>
/// <param name="Properties">The properties of the object, pre-ordered for serialization.</param>
internal sealed record SerializedObjectMeta (Type Clr,
    IReadOnlyList<SerializedPropertyMeta> Properties) : SerializedMeta(Clr);

/// <summary>
/// Describes a serializable property of a <see cref="SerializedObjectMeta"/>.
/// </summary>
internal sealed record SerializedPropertyMeta (Type Clr) : SerializedMeta(Clr)
{
    /// <summary>
    /// The name of the property.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// Corresponding JavaScript property name.
    /// </summary>
    public required string JSName { get; init; }
    /// <summary>
    /// Whether the property has the 'required' modifier.
    /// </summary>
    public required bool Required { get; init; }
    /// <summary>
    /// Whether the property should be omitted from serialization output when null.
    /// </summary>
    public required bool OmitWhenNull { get; init; }
    /// <summary>
    /// Whether the property is bound to a constructor parameter.
    /// </summary>
    public required bool ConstructorParameter { get; init; }
    /// <summary>
    /// How the property can be assigned during deserialization.
    /// </summary>
    public required SerializedPropertyKind Kind { get; init; }
    /// <summary>
    /// Name of the generated unsafe field accessor method when <see cref="SerializedPropertyKind.Field"/>.
    /// </summary>
    public required string? FieldAccessorName { get; init; }
}

/// <summary>
/// How a serialized property can be assigned during deserialization.
/// </summary>
internal enum SerializedPropertyKind
{
    /// <summary>
    /// Property cannot be set (read-only, no accessible setter or backing field).
    /// </summary>
    None,
    /// <summary>
    /// Property has a regular public setter.
    /// </summary>
    Set,
    /// <summary>
    /// Property has an init-only setter.
    /// </summary>
    Init,
    /// <summary>
    /// Property is set via an unsafe accessor to the compiler-generated backing field.
    /// </summary>
    Field
}
