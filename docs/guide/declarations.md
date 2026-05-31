# Type Declarations

Bootsharp will automatically generate [type declarations](https://www.typescriptlang.org/docs/handbook/2/type-declarations) for interop APIs when building the solution. One `.g.d.mts` file is emitted per C# namespace, colocated with the matching `.g.mjs` binding under the `generated/modules` directory of the compiled module package.

## Function Declarations

For interop methods, function declarations are emitted under the class's TS namespace wrapper inside the C# namespace's module:

```csharp
public class Class
{
    [Export]
    public static void Baz() { }
}
```

— will make the following emitted in `generated/modules/index.g.d.mts`:

```ts
export namespace Class {
    export function baz(): void;
}
```

— which allows consuming the API in JavaScript as:

```ts
import { Class } from "bootsharp";

Class.baz();
```

Imported methods will be emitted as properties, which have to be assigned before booting the runtime:

::: code-group

```csharp [Class.cs]
public partial class Class
{
    [Import]
    public static partial void Baz();
}
```

```ts [index.g.d.mts]
export namespace Class {
    export let baz: () => void;
}
```

```ts [main.ts]
import { Class } from "bootsharp";

Class.baz = () => {};
```

:::

## Overloaded Methods

JavaScript does not have function overloads, so Bootsharp automatically disambiguates them when projecting overloaded C# methods. The overload with the fewest parameters keeps the original name; the rest are suffixed with `With...` derived from the extra parameter names (or, when that is still ambiguous, from the full parameter names or parameter types).

::: code-group

```csharp [Class.cs]
public class Class
{
    [Export] public static void Start (string title) {}
    [Export] public static void Start (string title, string info) {}
    [Export] public static void Start (string title, double progress) {}
    [Export] public static void Start (string title, string info, double progress) {}
}
```

```ts [index.g.d.mts]
export namespace Class {
    export function start(title: string): void;
    export function startWithInfo(title: string, info: string): void;
    export function startWithProgress(title: string, progress: number): void;
    export function startWithInfoAndProgress(title: string, info: string, progress: number): void;
}
```

:::

## Default Arguments

C# method parameters with default values are emitted as optional TypeScript parameters using the `?:` syntax, letting callers omit them at the call site:

::: code-group

```csharp [Class.cs]
public class Class
{
    [Export]
    public static void Greet (string name, string greeting = "Hello") {}
}
```

```ts [index.g.d.mts]
export namespace Class {
    export function greet(name: string, greeting?: string): void;
}
```

```ts [main.ts]
import { Class } from "bootsharp";

Class.greet("World");
Class.greet("World", "Hi");
```

:::

## Property Declarations

Exported properties are emitted as variables under the declaring class's TS namespace:

::: code-group

```csharp [Class.cs]
public class Class
{
    [Export]
    public static string Baz { get; set; } = "";
}
```

```ts [index.g.d.mts]
export namespace Class {
    export let baz: string;
}
```

```ts [main.ts]
import { Class } from "bootsharp";

Class.baz = "updated";
```

:::

Imported properties are emitted as accessor pairs, which have to be assigned before booting the runtime:

::: code-group

```csharp [Class.cs]
public static partial class Class
{
    [Import]
    public static partial string Baz { get; set; }
}
```

```ts [index.g.d.mts]
export namespace Class {
    export let baz: { get: () => string; set: (value: string) => void };
}
```

```ts [main.ts]
import { Class } from "bootsharp";

let baz = "";
Class.baz = { get: () => baz, set: value => baz = value };
```

:::

## Event Declarations

Exported events are emitted as `EventSubscriber` objects:

::: code-group

```csharp [Class.cs]
public class Class
{
    [Export]
    public static event Action<string>? OnBaz;
}
```

```ts [index.g.d.mts]
export namespace Class {
    export const onBaz: EventSubscriber<[payload: string]>;
}
```

```ts [main.ts]
import { Class } from "bootsharp";

Class.onBaz.subscribe(payload => {});
```

:::

Imported events are emitted as `EventBroadcaster` objects:

::: code-group

```csharp [Class.cs]
public static partial class Class
{
    [Import]
    public static event Action<string>? OnBaz;
}
```

```ts [index.g.d.mts]
export namespace Class {
    export const onBaz: EventBroadcaster<[payload: string]>;
}
```

```ts [main.ts]
import { Class } from "bootsharp";

Class.onBaz.broadcast("updated");
```

:::

## Delegate Declarations

Custom delegates are emitted as TypeScript function-type aliases:

::: code-group

```csharp [Class.cs]
public delegate void Notify (string msg);

public class Class
{
    [Export]
    public static Notify GetNotify () => msg => Console.WriteLine(msg);
}
```

```ts [index.g.d.mts]
export type Notify = (msg: string) => void;

export namespace Class {
    export function getNotify(): Notify;
}
```

```ts [main.ts]
import { Class } from "bootsharp";

const notify = Class.getNotify();
notify("hello");
```

:::

Built-in `System.Action` and `System.Func` variants are supported as well:

::: code-group

```csharp [Class.cs]
public class Class
{
    [Export] public static Action<string>? Logger { get; set; }
}
```

```ts [index.g.d.mts]
export namespace Class {
    export let logger: system.Action<string> | undefined;
}
```

```ts [main.ts]
import { Class } from "bootsharp";

Class.logger = msg => console.log(msg);
Class.logger("hello");
```

:::

## Documentation Declarations

When an inspected assembly has XML documentation generated, Bootsharp mirrors the matching documentation into the emitted TypeScript declarations.

::: code-group

```csharp [MathApi.cs]
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

```ts [index.g.d.mts]
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

This is intentional and optimized for TypeScript ergonomics: `undefined` fits omitted or optional inputs, while `null` fits explicit data crossing the interop boundary.

## Namespaces

Members declared inside a C# namespace are emitted into a module path derived from that namespace: dots become path separators and casing is lower-kebab-cased. Members without a namespace land in the default `index` module (as shown in the examples above).

::: code-group

```csharp [Class.cs]
namespace Foo.Bar;

public class Class
{
    [Export]
    public static void Baz () { }
}
```

```ts [main.ts]
import { Class } from "bootsharp/foo/bar";

Class.baz();
```

:::

You can control how the C#-side namespace and type names resolve to the generated module and node names with the [rename attributes](/guide/renaming).

## Configuring Type Mappings

You can rename or omit the JavaScript node generated for an associated C# type via the [rename attributes](/guide/renaming).
