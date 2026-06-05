# Specialization

Bootsharp marshals every type automatically based on the convention: types with immutable/value semantics are [serialized by value](/guide/serialization), and others are [passed by reference](/guide/interop-instances).

It's possible to customize the behaviour with **specialization** are redefine how a particular CLR type crosses the interop boundary and what surface it exposes on the other side.

## How It Works

A specialization is a pair of classes describing a custom interop surface for a specific CLR type — one for each direction:

- **Export** (C# → JS) — a class annotated with `[SpecializeExport(typeof(T))]` and inherited from `SpecializedExport`. Bootsharp wraps an exported instance of the specialized type into this class before it crosses to JavaScript.
- **Import** (JS → C#) — an abstract class annotated with `[SpecializeImport(typeof(T))]` and inherited from `SpecializedImport`. Bootsharp uses it as the base of the generated interop proxy and treats its abstract members as the interop surface wired to JavaScript.

The two halves are paired: the export half implements every abstract member declared on the import half, so the same shape is exposed in both directions.

To override how Bootsharp marshals a type declare a specialization pair with the same attributes. Here's an example for `IComparer<T>`:

```csharp
[SpecializeImport(typeof(IComparer<>))]
public abstract class ComparerImport<T> (int id)
    : SpecializedImport(id), IComparer<T>
{
    public abstract int Compare (T? x, T? y);
}

[SpecializeExport(typeof(IComparer<>))]
public class ComparerExport<T> (IComparer<T> cmp)
    : SpecializedExport(cmp)
{
    public int Compare (T? x, T? y) => cmp.Compare(x, y);
}
```

The import half declares the interop surface (`Compare`) as abstract members; Bootsharp generates a proxy that forwards them to JavaScript and, since the class implements `IComparer<T>`, the proxy is usable as one on the C# side.

On the JavaScript side a comparer is just an object matching the declared surface:

```ts
import { Program } from "bootsharp";

Program.provideComparer = () => ({
    compare: (x, y) => x < y ? -1 : x > y ? 1 : 0
});

const comparer = Program.getComparer();
comparer.compare("a", "b"); // -1
```

## Injecting Code

The `[SpecializeImport]` attribute accepts optional `CS`, `JS`, `JSCtor` and `Decl` snippets that are spliced verbatim into the generated C# or JavaScript proxies and its TypeScript declaration. This lets the imported proxy satisfy JS-side contracts that aren't expressible through the C# abstract members alone — for example, injecting an iterator:

```csharp
[SpecializeImport(typeof(ICustomCollection<>),
    JS: "[Symbol.iterator]() { return this.copy()[Symbol.iterator](); }",
    Decl: "[Symbol.iterator](): IterableIterator<T>;")]
```

When `Decl` value starts with `export ` — the content will replace the entire TypeScript declaration of the type, instead of splicing it into the bottom of the default type declaration.

::: tip EXAMPLE
Find a more advanced example of injecting C# and JS constructor code to synthesise property events in the [E2E test project](https://github.com/elringus/bootsharp/tree/main/src/js/test/cs/Test.Library/Specialization.cs).
:::

## Unwrapping

An import specializer normally *is* the value handed to C# — the generated proxy implements the specialized interface, so it can stand in for it directly (the way `ComparerImport<T>` above serves as an `IComparer<T>`).

That doesn't work when the specialized type can't be implemented by a proxy, such as a value type like `CancellationToken`. In that case the proxy exposes the JavaScript-side surface as abstract members and overrides `SpecializedImport.Unwrap()` to build the concrete value from them:

```csharp
[SpecializeImport(typeof(CancellationToken))]
public abstract class CancellationTokenImport (int id) : SpecializedImport(id)
{
    public abstract bool IsCancellationRequested { get; }
    public abstract event Action OnCancellationRequested;

    private CancellationTokenSource? src;

    protected internal override object Unwrap ()
    {
        if (src != null) return src.Token;
        src = new();
        if (IsCancellationRequested) src.Cancel();
        else OnCancellationRequested += src.Cancel;
        return src.Token;
    }
}

[SpecializeExport(typeof(CancellationToken))]
public sealed class CancellationTokenExport : SpecializedExport
{
    public bool IsCancellationRequested => ct.IsCancellationRequested;
    public event Action? OnCancellationRequested;

    private readonly CancellationToken ct;

    public CancellationTokenExport (CancellationToken ct) : base(ct)
    {
        this.ct = ct;
        ct.Register(() => OnCancellationRequested?.Invoke());
    }
}
```

Bootsharp calls `Unwrap()` to obtain the value passed to C# — here a real `CancellationToken` backed by a source that's cancelled whenever the JavaScript token reports cancellation. The paired JavaScript class signals through the same surface:

```ts
import { CancellationToken } from "bootsharp";

const token = new CancellationToken();
token.cancel(); // fires onCancellationRequested
```

## Reference

The built-in specializations live in [`Specialized.cs`](https://github.com/elringus/bootsharp/blob/main/src/cs/Bootsharp.Common/Specialization/Specialized.cs); their JavaScript counterparts are the modules under [`src/js/src/bcl`](https://github.com/elringus/bootsharp/tree/main/src/js/src/bcl). Use them as a template when authoring your own.
