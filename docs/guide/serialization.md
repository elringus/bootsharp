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

When a value of non-natively supported type is specified in an interop API, Bootsharp will attempt to de-/serialize it with [System.Text.JSON](https://learn.microsoft.com/en-us/dotnet/api/system.text.json) using fast source-generation mode. The whole process is encapsulated under the hood on both the C# and JavaScript sides, so you don't have to manually author generator hints or specify `[MarshallAs]` attributes for each value:

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

ES6 [Map](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Map) doesn't natively support JSON serialization, hence Bootsharp will use plain objects when serializing C# dictionaries:

```csharp
[JSInvokable]
public static Dictionary<string, bool> GetMap () =>
    new () { ["foo"] = true, ["bar"] = false };
```

— the dictionary can be accessed via keys as usual JavaScript object:

```ts
import { Program } from "bootsharp";

const map = Program.getMap();
console.log(map.foo); // true
console.log(map["bar"]); // false
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
console.log(map.bar); // 7
```
