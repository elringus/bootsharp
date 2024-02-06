# Introduction

## What?

Bootsharp is a solution for building web applications, where domain is authored in .NET C# and is consumed by a standalone JavaScript or TypeScript project.

## Why?

C# is a popular language for building maintainable software with complex domain logic, such as enterprise and financial applications. However, its frontend capabilities are lacking, especially compared to the web ecosystem.

Web platform is the industry-standard for building modern user interfaces. It has best in class tooling and frameworks, such as [React](https://react.dev) and [Svelte](https://svelte.dev) — allowing to build better frontends faster, compared to any other language/platform ecosystem.

In contrast to solutions like [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor), which attempt to bring the entire web platform inside .NET (effectively reversing the natural workflow), Bootsharp facilitates high-level interoperation between C# and TypeScript, allowing to build domain and UI layers under their natural ecosystems.

## How?

Bootsharp installs as a NuGet package to the C# project dedicated for building the solution for web. It takes special care to not "leak" the dependency outside entry assembly of the web target, which is essential to keep the domain clean from any platform-specific details.

While it's possible to author both export (C# -> JavaScript) and import (JavaScript -> C#) bindings via static methods, complex solutions will benefit from interface-based interop: simply feed Bootsharp C# interfaces describing export and import API surfaces, and it will automatically generate associated bindings and type declarations.

![](/img/banner.png)

Bootsharp will automatically build and bundle JavaScript package when publishing C# solution, as well as generate `package.json`, so that you can reference the whole C# solution as any other ES module in your web project.

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
