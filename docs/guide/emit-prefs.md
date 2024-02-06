# Emit Preferences

## Space

By default, all the generated JavaScript binding objects and TypeScript declarations are grouped under corresponding C# namespaces.

To override the generated namespaces, apply `JSNamespace` attribute to the entry assembly of the C# program. The attribute expects `pattern` and `replacement` arguments, which are provided to [Regex.Replace](https://docs.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex.replace?view=net-6.0#system-text-regularexpressions-regex-replace(system-string-system-string-system-string)) when building the generated namespace name.

For example, to transform `Company.Product.Space` into `Space` namespace, use the following pattern:

```csharp
[assembly:JSNamespace(@"Company\.Product\.(\S+)", "$1")]
```

## Function

Both export and import attributes have additional parameters allowing to override generated binding methods names and invocation bodies.

Let's say we want to rename methods starting with `Notify...` to `On...` on the JS side to make it more clear that we are notifying on C# side and consuming events on JS:

```csharp
[assembly: JSImport(new[] {
    ...
}, namePattern: "Notify(.+)", nameReplacement: "On$1")]
```

Or maybe we want to wrap the exported bindings with some kind of error-catching mechanism:

```csharp
[assembly: JSExport(new[] {
    ...
}, invokePattern: "(.+)", invokeReplacement: "Try(() => $1)")]
```
