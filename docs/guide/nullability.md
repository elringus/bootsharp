# Nullability

Bootsharp accepts both `null` and `undefined` when JavaScript sends a nullable value to C#. On the way back, generated TypeScript declarations use a contextual convention instead of mirroring the original JavaScript token.

This keeps the API surface predictable:

- Nullable method arguments are declared as `| undefined`
- Nullable object properties are declared as optional with `?`
- Nullable method return values are declared as `| null`
- Nullable collection elements and dictionary values are declared as `| null`
- Event payloads use `undefined` for missing nullable arguments

## Why The Convention Is Split

`undefined` works best in TypeScript when the value is omitted by the caller, such as an optional argument or an absent object property. `null` works better when the value is part of actual data crossing the interop boundary, such as a returned result, or an array element.

Because C# has a single null concept, Bootsharp treats `null` and `undefined` as the same nullable input when serializing from JavaScript. The generated declarations then pick the most ergonomic representation for each position in the API surface.

## Method Arguments

Nullable method arguments are emitted as `| undefined`.

::: code-group

```csharp [C#]
[JSInvokable]
public static void SetTitle (string? title) { }
```

```ts [TypeScript]
export namespace Program {
    export function setTitle(title: string | undefined): void;
}
```

```ts [Usage]
Program.setTitle("Bootsharp");
Program.setTitle(undefined);
```

:::

Passing `null` will also work at runtime, but the declaration prefers `undefined` because that is the more natural TypeScript representation for omitted optional input.

## Return Values

Nullable method return values are emitted as `| null`.

::: code-group

```csharp [C#]
[JSInvokable]
public static string? FindUserName (int id) => null;
```

```ts [TypeScript]
export namespace Program {
    export function findUserName(id: number): string | null;
}
```

```ts [Usage]
const name = Program.findUserName(7);
if (name === null)
    console.log("User not found.");
```

:::

Nullable return values use `null` because the JavaScript caller is consuming an explicit result produced by C#.

## Object Properties

Nullable object properties are emitted as optional properties.

::: code-group

```csharp [C#]
public record User (string Name, string? Nickname);
```

```ts [TypeScript]
export interface User {
    name: string;
    nickname?: string;
}
```

```ts [Usage]
const a: User = { name: "Nina" };
const b: User = { name: "Nina", nickname: "Nin" };
```

:::

This matches normal TypeScript object ergonomics: missing nullable properties are omitted instead of represented with explicit `null` fields.

## Collection Elements

Nullable collection elements are emitted as `| null`.

::: code-group

```csharp [C#]
[JSInvokable]
public static string?[]? EchoNames (string?[]? names) => names;

[JSInvokable]
public static List<int?>? EchoNumbers (List<int?>? numbers) => numbers;
```

```ts [TypeScript]
export namespace Program {
    export function echoNames(names: Array<string | null> | undefined): Array<string | null> | null;
    export function echoNumbers(numbers: Array<number | null> | undefined): Array<number | null> | null;
}
```

```ts [Usage]
Program.echoNames(["Alice", null, "Bob"]);
Program.echoNumbers([1, null, 3]);
```

:::

`null` is preferred here because collection slots are explicit data. `Array<T | undefined>` would suggest sparse arrays or omitted elements, which is not what C# collections model.

## Dictionary Values

Nullable dictionary values are also emitted as `| null`.

::: code-group

```csharp [C#]
[JSInvokable]
public static Dictionary<string, string?>? GetLabels () =>
    new () { ["a"] = "Ready", ["b"] = null };
```

```ts [TypeScript]
export namespace Program {
    export function getLabels(): Map<string, string | null> | null;
}
```

```ts [Usage]
const labels = Program.getLabels();
const label = labels?.get("b");
if (label === null)
    console.log("Label is explicitly empty.");
```

:::

Using `null` here avoids ambiguity with `Map.get`, which already returns `undefined` when the key is missing.

## Events

Events are the one special case where missing nullable payload values are exposed as `undefined`.

::: code-group

```csharp [C#]
[JSEvent]
public static partial void OnVehicleChanged (int id, Vehicle? vehicle);
```

```ts [TypeScript]
export namespace Program {
    export const onVehicleChanged: Event<[id: number, vehicle: Vehicle | undefined]>;
}
```

```ts [Usage]
Program.onVehicleChanged.subscribe((id, vehicle) => {
    if (vehicle === undefined)
        console.log(`Vehicle ${id} was removed.`);
});
```

:::

This is intentional. Event payloads behave like arguments provided to `broadcast(...)`, so `undefined` feels more natural on the JavaScript side.
