namespace Bootsharp;

/// <summary>
/// Type of interop interface.
/// </summary>
public enum InterfaceKind
{
    /// <summary>
    /// The interface was supplied under <see cref="JSExportAttribute"/> and
    /// is intended for exposing C# APIs to JavaScript.
    /// </summary>
    Export,
    /// <summary>
    /// The interface was supplied under <see cref="JSImportAttribute"/> and
    /// is intended for exposing JavaScript APIs to C#.
    /// </summary>
    Import
}
