<p align="center">
  <a href="https://sharp.elringus.com" target="_blank" rel="noopener noreferrer">
    <img width="200" src="https://raw.githubusercontent.com/elringus/bootsharp/main/docs/public/favicon.svg" alt="Bootsharp">
  </a>
</p>
<br/>
<p align="center">
  <a href="https://www.nuget.org/packages/Bootsharp"><img src="https://img.shields.io/nuget/v/Bootsharp" alt="nuget"></a>
  <a href="https://codefactor.io/repository/github/elringus/bootsharp/overview/main"><img src="https://codefactor.io/repository/github/elringus/bootsharp/badge/main" alt="codefactor"></a>
  <a href="https://codecov.io/gh/elringus/bootsharp"><img src="https://codecov.io/gh/elringus/bootsharp/branch/main/graph/badge.svg?token=AAhei51ETt" alt="codecov"></a>
  <a href="https://github.com/elringus/bootsharp/actions/workflows/codeql.yml"><img src="https://github.com/elringus/bootsharp/actions/workflows/codeql.yml/badge.svg" alt="codeql"></a>
</p>
<br/>

# Use C# in web apps with comfort

Bootsharp streamlines consuming .NET C# apps and libraries in web projects. It's ideal for building web applications, where domain (backend) is authored in .NET C#, while the UI (frontend) is a standalone TypeScript or JavaScript project. Think of it as [Embind](https://emscripten.org/docs/porting/connecting_cpp_and_javascript/embind.html) for C++ or [wasm-bindgen](https://github.com/rustwasm/wasm-bindgen) for Rust.

![](https://raw.githubusercontent.com/elringus/bootsharp/main/docs/public/img/banner.png)

## Features

✨ High-level C# <-> TypeScript interop

📦 Embeds binaries to single-file ES module

🗺️ Works in browsers and JS runtimes (Node, Deno, Bun)

⚡ Generates bindings and types over C# interfaces

🏷️ Supports interop over object instances

🛠️ Allows customizing emitted bindings

🔥 Supports WASM multi-threading, AOT, trimming


## 🎬 Get Started

https://sharp.elringus.com/guide/getting-started

### Why not Blazor?

In contrast to solutions like Blazor, which attempt to bring the entire web platform inside .NET, Bootsharp facilitates high-level interoperation between C# and TypeScript, allowing to build the UI layer under its natural ecosystem using industry-standard tooling and frameworks, such as [React](https://react.dev) and [Svelte](https://svelte.dev).

### Why not `System.JavaScript`?

Bootsharp itself is built on top of [System.Runtime.InteropServices.JavaScript](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/import-export-interop?view=aspnetcore-8.0) introduced in .NET 7.

If you need to expose a simple library API to JavaScript and don't require type declarations, Bootsharp is probably overkill. However, .NET's interop is low-level, lacks support for passing custom types by value, and requires extensive boilerplate to define bindings, making it impractical for large API surfaces.

With Bootsharp, you can simply provide your domain-specific interfaces and use them seamlessly on the other side, as if they were originally authored in TypeScript (and vice versa). This ensures a clear separation of concerns: your domain codebase won't be aware of the JavaScript environment—no need to annotate methods for interop or specify marshalling hints for arguments.

For example, consider the following abstract domain code:

```cs
public record Data (string Info, IReadOnlyList<Item> Items);
public record Result (View Header, View Content);
public interface IProvider { Data GetData (); }
public interface IGenerator { Result Generate (); }

public class Generator (IProvider provider) : IGenerator
{
    public Result Generate ()
    {
        var data = provider.GetData();
        // Process the data and generate result.
        return result;
    }
}
```
— the code doesn't use any JavaScript-specific APIs, making it fully testable and reusable. To expose it to JavaScript, all we need to do is add the following to `Program.cs` in a separate project for the WASM target:

```cs
using Bootsharp;
using Bootsharp.Inject;
using Microsoft.Extensions.DependencyInjection;

[assembly: JSImport(typeof(IProvider))]
[assembly: JSExport(typeof(IGenerator))]

// Bootsharp auto-injects implementation for 'IProvider'
// from JS and exposes 'Generator' APIs to JS.
new ServiceCollection()
    .AddBootsharp()
    .AddSingleton<IGenerator, Generator>()
    .BuildServiceProvider()
    .RunBootsharp();
```

— we can now provide implementation for `IProvider` and use `Generator` in JavaScript/TypeScript:

```ts
import bootsharp, { Provider, Generator } from "bootsharp";

// Implement 'IProvider'.
Provider.getData = () => ({
    info: "...",
    items: []
});

await bootsharp.boot();

// Use 'Generator'.
const result = Generator.generate();
```
