# Emit Preferences

Use `[JSPreferences]` assembly attribute to customize Bootsharp behaviour at build time when the interop code is emitted. It has several properties that takes array of `(pattern, replacement)` strings, which are feed to [Regex.Replace](https://docs.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex.replace?view=net-6.0#system-text-regularexpressions-regex-replace(system-string-system-string-system-string)) when emitted associated code. Each consequent pair is tested in order; on first match the result replaces the default.

## Space

By default, all the generated JavaScript binding objects and TypeScript declarations are grouped under corresponding C# namespaces; refer to [namespaces](/guide/namespaces) docs for more info.

To customize emitted spaces, use `Space` parameter. For example, to make all bindings declared under "Foo.Bar" C# namespace have "Baz" namespace in JavaScript:

```cs
[assembly: JSPreferences(
    Space = ["^Foo\.Bar\.(\S+)", "Baz.$1"]
)]
```

The patterns are matched against full type name of declaring C# type when generating JavaScript objects for interop methods and against namespace when generating TypeScript syntax for C# types. Matched type names have the following modifications:

- interfaces have first character removed
- generics have parameter spec removed
- nested type names have `+` replaced with `.`

## Type

Allows customizing generated TypeScript type syntax. The patterns are matched against full C# type names of interop method arguments, return values and object properties.

## Event

Used to customize which C# methods should be transformed into JavaScript events, as well as generated event names. The patterns are matched against C# method names declared under `[JSImport]` interfaces. By default, methods starting with "Notify..." are matched and renamed to "On...".

## Function

Customizes generated JavaScript function names. The patterns are matched against C# interop method names.
