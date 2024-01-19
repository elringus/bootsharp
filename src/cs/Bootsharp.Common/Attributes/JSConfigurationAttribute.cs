namespace Bootsharp;

/// <summary>
/// When applied to WASM entry point assembly, configures Bootsharp behaviour at build time.
/// </summary>
/// <remarks>
/// Override a method in the inherited <see cref="Preferences"/> class to configure associated
/// operation. Each method has "default" argument containing default result of the operation;
/// return custom result to override it or return it as-is to keep default behaviour.
/// </remarks>
/// <example>
/// Make generated JS namespaces equal last part of the associated C# namespace:
/// <code><![CDATA[
/// [assembly: JSConfiguration<MyPrefs>]
///
/// public class MyPrefs : Preferences
/// {
///      public override string ResolveSpace (Type type, string @default)
///      {
///           var lastDotIdx = @default.LastIndexOf('.');
///           if (lastDotIdx >= 0) return @default[lastDotIdx..];
///           return @default;
///      }
/// }
/// ]]></code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class JSConfigurationAttribute<T> : Attribute where T : Preferences, new();
