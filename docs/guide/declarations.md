# Type Declarations

Bootsharp will automatically generate [type declarations](https://www.typescriptlang.org/docs/handbook/2/type-declarations) for interop APIs when building the solution. The files are emitted under "types" directory of the compiled module package.

## Function Declarations

For the interop methods, function declarations are emitted.

Exported `[JSInvokable]` methods will have associated function assigned under the declaring type space:

```csharp
public class Foo
{
    [JSInvokable]
    public static void Bar() { }
}
```

— will make following emitted in the declaration file:

```ts
export namespace Foo {
    export function bar(): void;
}
```

— which allows consuming the API in JavaScript as follows:

```ts
import { Foo } from "bootsharp";

Foo.bar();
```

Imported `[JSFunction]` methods will be emitted as properties, which have to be assigned before booting the runtime:

::: code-group

```csharp [Foo.cs]
public partial class Foo
{
    [JSFunction]
    public static partial void Bar();
}
```

```ts [bindings.d.ts]
export namespace Foo {
    export let bar: () => void;
}
```

```ts [main.ts]
import { Foo } from "bootsharp";

Foo.bar = () => {};
```

:::

## Event Declarations

`[JSEvent]` methods will be emitted as objects with `subscribe` and `unsubscribe` methods:

::: code-group

```csharp [Foo.cs]
public class Foo
{
    [JSEvent]
    public static partial void OnBar (string payload);
}
```

```ts [bindings.d.ts]
export namespace Foo {
    export const onBar: Event<[string]>;
}
```

```ts [main.ts]
import { Foo } from "bootsharp";

Foo.onBar.subscribe(pyaload => {});
```

:::

## Type Crawling

Bootsharp will crawl types from the interop signatures and mirror them in the emitted declarations. For example, if you have a custom record with property of another custom record implementing a custom interface, both records and the interface will be emitted:

::: code-group

```csharp [Foo.cs]
public interface IFoo { };
public record Foo : IFoo;
public record Bar (Foo foo);

public partial class Foo
{
    [JSFunction]
    public static partial Bar GetBar();
}
```

```ts [bindings.d.ts]
export interface IFoo {}
export interface Foo implements IFoo {}
export interface Bar {foo: Foo;}

export namespace Foo {
    export function getBar(): Bar;
}
```

:::

## Configuring Type Mappings

You can override which type declaration are generated for associated C# types via `Type` patterns of [emit preferences](/guide/emit-prefs).
