# Introduction

## What?

Bootsharp is a solution for building web applications where the domain logic is authored in .NET C# and consumed by a standalone JavaScript or TypeScript project.

## Why?

C# is a popular choice for building maintainable software with complex domain logic, especially in enterprise and financial systems. However, its frontend capabilities are limited—particularly when compared to what the web ecosystem offers.

The web platform is the industry standard for modern UI development. Frameworks such as [React](https://react.dev) and [Svelte](https://svelte.dev) provide exceptional tooling, fast iteration, and a vast ecosystem, enabling developers to build high-quality interfaces with ease.

Solutions like [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) attempt to bring the entire web platform into .NET, effectively reversing the natural workflow and restricting access to native JavaScript tools. Bootsharp takes the opposite approach: it enables high-level interoperation between C# and TypeScript, so each layer can be developed within its optimal environment.

With Bootsharp, you implement domain logic in C# and build the UI using familiar web technologies, then connect them seamlessly. Your project can be published to the web or bundled as a native desktop or mobile application using [Electron](https://electronjs.org) or [Tauri](https://tauri.app).

## How?

Bootsharp is installed as a [NuGet package](https://www.nuget.org/packages/Bootsharp) into the C# project dedicated to building the solution for the web. It is specifically designed not to "leak" the dependency outside the entry assembly of the web target—essential for keeping the domain clean of any platform-specific details.

While it's possible to author both export (C# → JS) and import (C# ← JS) bindings via static methods, complex solutions benefit from interface-based interop. Simply provide Bootsharp with C# interfaces describing the export and import API surfaces, and it will automatically generate the associated bindings and type declarations.

![](/img/banner.png)

Bootsharp will automatically build and bundle the JavaScript package when publishing the C# solution, and generate a `package.json`, allowing you to reference the entire C# solution as any other ES module in your web project.

::: code-group
```jsonc [package.json]
"scripts": {
    // Compile C# solution into ES module.
    "compile": "dotnet publish backend"
},
"dependencies": {
    // Reference C# solution module.
    "backend": "file:backend"
}
```
:::

::: code-group
```ts [main.ts]
// Import C# solution module.
import bootsharp, { Backend, Frontend } from "backend";

// Boot C# WASM module.
await boosharp.boot();

// Subscribe to C# event.
Frontend.onUserChanged.subscribe(updateUserUI);

// Invoke C# method.
Backend.addUser({ name: "Carl" });
```
:::
