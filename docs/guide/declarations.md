# Type Declarations

Bootsharp will automatically generate [type declarations](https://www.typescriptlang.org/docs/handbook/2/type-declarations) for interop APIs when building the solution. One `.g.d.mts` file is emitted per C# namespace, colocated with the matching `.g.mjs` binding under the `generated/` directory of the compiled module package.

## Function Declarations

For interop methods, function declarations are emitted under the class's TS namespace wrapper inside the C# namespace's module:

```csharp
namespace Foo;

public class Bar
{
    [Export]
    public static void Baz() { }
}
```

— will make the following emitted in `generated/foo.g.d.mts`:

```ts
export namespace Bar {
    export function baz(): void;
}
```

— which allows consuming the API in JavaScript as:

```ts
import { Bar } from "bootsharp/foo";

Bar.baz();
```

Imported methods will be emitted as properties, which have to be assigned before booting the runtime:

::: code-group

```csharp [Bar.cs]
namespace Foo;

public partial class Bar
{
    [Import]
    public static partial void Baz();
}
```

```ts [foo.g.d.mts]
export namespace Bar {
    export let baz: () => void;
}
```

```ts [main.ts]
import { Bar } from "bootsharp/foo";

Bar.baz = () => {};
```

:::

## Property Declarations

Exported properties are emitted as variables under the declaring class's TS namespace:

::: code-group

```csharp [Bar.cs]
namespace Foo;

public class Bar
{
    [Export]
    public static string Baz { get; set; } = "";
}
```

```ts [foo.g.d.mts]
export namespace Bar {
    export let baz: string;
}
```

```ts [main.ts]
import { Bar } from "bootsharp/foo";

Bar.baz = "updated";
```

:::

Imported properties are emitted as accessor pairs, which have to be assigned before booting the runtime:

::: code-group

```csharp [Bar.cs]
namespace Foo;

public static partial class Bar
{
    [Import]
    public static partial string Baz { get; set; }
}
```

```ts [foo.g.d.mts]
export namespace Bar {
    export let baz: { get: () => string; set: (value: string) => void };
}
```

```ts [main.ts]
import { Bar } from "bootsharp/foo";

let baz = "";
Bar.baz = { get: () => baz, set: value => baz = value };
```

:::

## Event Declarations

Exported events are emitted as `EventSubscriber` objects:

::: code-group

```csharp [Bar.cs]
namespace Foo;

public class Bar
{
    [Export]
    public static event Action<string>? OnBaz;
}
```

```ts [foo.g.d.mts]
export namespace Bar {
    export const onBaz: EventSubscriber<[payload: string]>;
}
```

```ts [main.ts]
import { Bar } from "bootsharp/foo";

Bar.onBaz.subscribe(payload => {});
```

:::

Imported events are emitted as `EventBroadcaster` objects:

::: code-group

```csharp [Bar.cs]
namespace Foo;

public static partial class Bar
{
    [Import]
    public static event Action<string>? OnBaz;
}
```

```ts [foo.g.d.mts]
export namespace Bar {
    export const onBaz: EventBroadcaster<[payload: string]>;
}
```

```ts [main.ts]
import { Bar } from "bootsharp/foo";

Bar.onBaz.broadcast("updated");
```

:::

## Documentation Declarations

When an inspected assembly has XML documentation generated, Bootsharp mirrors the matching documentation into the emitted TypeScript declarations.

::: code-group

```csharp [MathApi.cs]
namespace Foo;

/// <summary>Math API.</summary>
public class MathApi
{
    /// <summary>Adds two numbers.</summary>
    /// <param name="left">Left number.</param>
    /// <param name="right">Right number.</param>
    /// <returns>The sum.</returns>
    [Export]
    public static int Add (int left, int right) => left + right;
}
```

```ts [foo.g.d.mts]
/**
 * Math API.
 */
export namespace MathApi {
    /**
     * Adds two numbers.
     * @param left Left number.
     * @param right Right number.
     * @returns The sum.
     */
    export function add(left: number, right: number): number;
}
```

:::

## Nullability

Bootsharp uses different TypeScript nullish forms depending on where a nullable C# value appears:

- nullable method arguments become `| undefined`
- nullable properties become optional with `?`
- nullable return values become `| null`
- nullable collection elements and dictionary values become `| null`

This is intentional and optimized for TypeScript ergonomics. Refer to the dedicated [nullability guide](/guide/nullability) for the full convention and examples.

## Type Crawling

Bootsharp will crawl types from the interop signatures and mirror them as top-level exports of the same C# namespace's declaration module. For example, if you have a custom record with a property of another custom record implementing a custom interface, both records and the interface will be emitted:

::: code-group

```csharp [Foo.cs]
namespace Space;

public interface IFoo { };
public record Foo : IFoo;
public record Bar (Foo foo);

public partial class Holder
{
    [Import]
    public static partial Bar GetBar();
}
```

```ts [space.g.d.mts]
export interface IFoo {}
export type Foo = IFoo & Readonly<{}>;
export type Bar = Readonly<{
    foo: Foo;
}>;

export namespace Holder {
    export function getBar(): Bar;
}
```

:::

## Configuring Type Mappings

You can override which type declaration is generated for associated C# types via `Type` patterns of [emit preferences](/guide/preferences).
