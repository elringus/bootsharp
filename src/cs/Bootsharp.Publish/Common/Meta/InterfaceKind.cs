namespace Bootsharp.Publish;

/// <summary>
/// The type of API interop interface represents.
/// </summary>
internal enum InterfaceKind
{
    /// <summary>
    /// The interface represents C# API consumed in JavaScript.
    /// </summary>
    Export,
    /// <summary>
    /// The interface represents JavaScript API consumed in C#.
    /// </summary>
    Import
}
