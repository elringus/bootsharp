# Preferences

Use `[Preferences]` assembly attribute to customize Bootsharp behaviour at build time when the interop code is emitted. It has several properties that takes array of `(pattern, replacement)` strings, which are feed to [Regex.Replace](https://docs.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex.replace?view=net-6.0#system-text-regularexpressions-regex-replace(system-string-system-string-system-string)) when emitted associated code. Each consequent pair is tested in order; on first match the result replaces the default.

## Space

By default, all the generated JavaScript binding objects and TypeScript declarations are grouped under corresponding C# namespaces; refer to [namespaces](/guide/namespaces) docs for more info.

To customize emitted spaces, use `Space` parameter. For example, to make all bindings declared under "Foo.Bar" C# namespace have "Baz" namespace in JavaScript:

```cs
[assembly: Preferences(
    Space = ["^Foo\.Bar\.(\S+)", "Baz.$1"]
)]
```

The patterns are matched against the C# full type name. Nested types use `+` as separator; generic types include the arity suffix.

## Type

Allows customizing generated TypeScript type syntax. The patterns are matched against full C# type names of interop method arguments, return values and object properties.

## Function

Customizes generated JavaScript function names. The patterns are matched against C# interop method names.
