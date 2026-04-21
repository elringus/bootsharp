# Serialization

Most simple types, such as numbers, booleans, strings, arrays (lists) and promises (tasks) of them are marshalled in-memory when crossing the C# <-> JavaScript boundary. Below are some of the natively-supported types (refer to .NET docs for the [full list](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/import-export-interop)):

| C#       | JavaScript | Task of | Array of |
|----------|------------|:-------:|:--------:|
| bool     | boolean    |   ✔️    |    ❌     |
| byte     | number     |   ✔️    |    ✔️    |
| char     | string     |   ✔️    |    ❌     |
| string   | string     |   ✔️    |    ✔️    |
| int      | number     |   ✔️    |    ✔️    |
| long     | BigInt     |   ✔️    |    ❌     |
| float    | Number     |   ✔️    |    ❌     |
| DateTime | Date       |   ✔️    |    ❌     |

When a value of non-natively supported type is specified in an interop API, Bootsharp will de-/serialize it using a custom efficient binary serialization format. The whole process is encapsulated under the hood on both the C# and JavaScript sides, so you don't have to manually author generator hints or specify `[MarshallAs]` attributes for each value:

```csharp
public record User (long Id, string Name, DateTime Registered);

[JSInvokable]
public static void AddUser (User user) { }

[JSEvent]
public static partial void OnUserModified (User user);
```

— Bootsharp will automatically emit C# and JavaScript code required to de-/serialize `User` record on both ends, so that you can consume the APIs as if they were initially authored in JavaScript:

```ts
import { Program } from "bootsharp";

Program.addUser({ id: 17, name: "Carl", registered: Date.now() });

Program.onUserModified.subscribe(handleUserModified);

function handleUserModified(user: Program.User) { }
```

## Enums Serialization

Enums are marshalled as numbers for better performance, while additional name <-> index mappings are emitted on the JavaScript side for convenience.

```csharp
public enum Options { Foo, Bar }

[JSInvokable]
public static Options GetOption () => Options.Bar;
```

— while "GetOptions" return value will be passed to JavaScript as an integer index, Bootsharp will map enum indexes to string values (and vice-versa) in the emitted code, so that following will work as expected:

```ts
import { Program } from "bootsharp";

const option = Program.getOption();
console.log(option === Program.Options.Foo); // false
console.log(option === Program.Options.Bar); // true
console.log(Program.Options[Program.Options.Foo]); // "Foo"
console.log(Program.Options[1]); // "Bar"
```

## Dictionary Serialization

Bootsharp marshals C# dictionaries as ES6 [Map](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Map):

```csharp
[JSInvokable]
public static Dictionary<string, bool> GetMap () =>
    new () { ["foo"] = true, ["bar"] = false };
```

— the dictionary can be accessed with standard `Map` APIs:

```ts
import { Program } from "bootsharp";

const map = Program.getMap();
console.log(map.get("foo")); // true
console.log(map.get("bar")); // false
```

## Collection Interfaces

It's common to use various collection interfaces, such as `IReadOnlyList` or `IReadOnlyDictionary` when authoring C# APIs. Bootsharp will accept any kind of array or dictionary compatible interface in the interop APIs and marshal them as plain arrays and maps by default:

```csharp
[JSInvokable]
public static IReadOnlyDictionary<string, float> Map (
    IReadOnlyList<string> a, IReadOnlyCollection<float> b) { }
```

```ts
import { Program } from "bootsharp";

const map = Program.map(["foo", "bar"], [0, 7]);
console.log(map.get("bar")); // 7
```

## Computed Properties

Computed C# properties are evaluated and written to the JS objects on serialization. For example:

```csharp
public record Order
{
    public required string Id { get; init; }
    public required int Revision { get; init; }
    public string Version => $"{Id}:{Revision}";
}
```

When an `Order` crosses the C# -> JavaScript boundary, Bootsharp reads `Version` on the C# side and writes the result to the JavaScript object as a regular persisted value:

```ts
const order = Orders.getCurrent();
console.log(order.version); // "A:7"
```

The value is computed only while the C# object is being serialized. After that, it's just a normal JavaScript property on the received object.
