# Introduction

## What?

Bootsharp is a solution for building web applications where the domain logic is authored in .NET C# and consumed by a standalone JavaScript or TypeScript project.

## Why?

C# is a popular language for building maintainable software with complex domain logic, such as enterprise and financial applications. However, its frontend capabilities are lacking—especially when compared to the web ecosystem.

The web platform is the industry standard for building modern user interfaces. It offers best-in-class tooling and frameworks, such as [React](https://react.dev) and [Svelte](https://svelte.dev), enabling developers to build better frontends faster than with any other language or platform.

In contrast to solutions like [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor), which attempt to bring the entire web platform into .NET (effectively reversing the natural workflow), Bootsharp facilitates high-level interoperation between C# and TypeScript. This allows you to build the domain and UI layers within their natural ecosystems. The project can then be published to the web or bundled as a native desktop or mobile application with [Electron](https://electronjs.org) or [Tauri](https://tauri.app).

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
