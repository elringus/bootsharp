# Getting Started

## Configure C# Project

In `.csproj` file, set wasm runtime identifier and reference Bootsharp package:

```xml

<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Bootsharp" Version="*-*"/>
    </ItemGroup>

</Project>
```

## Author Interop APIs

Specify interop surface in the C# project.

```cs
using System;
using Bootsharp;

public static partial class Program
{
    public static void Main ()
    {
        OnMainInvoked($"Hello {GetFrontendName()}, .NET here!");
    }

    [JSEvent] // Used in JS as Program.onMainInvoked.subscribe(..)
    public static partial void OnMainInvoked (string message);

    [JSFunction] // Set in JS as Program.getFrontendName = () => ..
    public static partial string GetFrontendName ();

    [JSInvokable] // Invoked from JS as Program.GetBackendName()
    public static string GetBackendName () => Environment.Version;
}
```

::: info NOTE
Authoring interop via static methods is impractical for large API surfaces—it's shown here only as a simple way to get started. For real projects, consider using [interop interfaces](/guide/interop-interfaces) instead.
:::

## Compile ES Module

Run following command under the solution root:

```sh
dotnet publish
```

— which will produce `bin/bootsharp` directory with the following content:

| Name         | Type   | Description                                               |
|--------------|--------|-----------------------------------------------------------|
| types        | folder | Contains type declarations for the authored interop APIs. |
| index.mjs    | file   | The compiled ES module with embedded binaries.            |
| package.json | file   | NPM package manifest for convenient importing.            |

## Consume C# APIs in JavaScript

Import the compiled ES module, assign imported functions, boot the runtime and use exported methods:

::: code-group

```js [JavaScript Runtime (Node, Deno, Bun)]
// Importing compiled ES module.
import bootsharp, { Program } from "./bin/bootsharp/index.mjs";

// Binding 'Program.GetFrontendName' import invoked in C#.
Program.getFrontendName = () => process.version;

// Subscribing to 'Program.OnMainInvoked' C# event.
Program.onMainInvoked.subscribe(console.log);

// Initializing dotnet runtime and invoking entry point.
await bootsharp.boot();

// Invoking 'Program.GetBackendName' C# method.
console.log(`Hello ${Program.getBackendName()}!`);
```

```html [Web Browser]
<!DOCTYPE html>

<script type="module">

    // Importing compiled ES module.
    import bootsharp, { Program } from "./bin/bootsharp/index.mjs";

    // Binding 'Program.GetFrontendName' import invoked in C#.
    Program.getFrontendName = () => "Browser";

    // Subscribing to 'Program.OnMainInvoked' C# event.
    Program.onMainInvoked.subscribe(console.log);

    // Initializing dotnet runtime and invoking entry point.
    await bootsharp.boot();

    // Invoking 'Program.GetBackendName' C# method.
    console.log(`Hello ${Program.getBackendName()}!`);

</script>
```

:::

## Run the App

Assuming the above code is in `main.mjs` file for JavaScript runtimes or in `index.html` file for browser, run the following to test the app:

::: code-group

```sh [Node]
node main.mjs
```

```sh [Deno]
deno run main.mjs
```

```sh [Bun]
bun main.mjs
```

```sh [Browser]
npx serve
```

:::

::: tip EXAMPLE
Find full sources of the minimal sample on GitHub: https://github.com/elringus/bootsharp/tree/main/samples/minimal.
:::
