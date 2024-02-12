<p align="center">
  <a href="https://bootsharp.com" target="_blank" rel="noopener noreferrer">
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

# Features

✨ High-level C# <-> TypeScript interop

📦 Embeds binaries to single-file ES module

🗺️ Works in browsers and JS runtimes (Node, Deno, Bun)

⚡ Generated bindings and types over C# interfaces

🏷️ Supports interop over object instances

🛠️ Allows customizing emitted bindings

🔥 Supports WASM multi-threading, AOT, trimming

# Why not Blazor?

In contrast to solutions like Blazor, which attempt to bring the entire web platform inside .NET, Bootsharp facilitates high-level interoperation between C# and TypeScript, allowing to build the UI layer under its natural ecosystem using industry-standard tooling and frameworks, such as [React](https://react.dev) and [Svelte](https://svelte.dev).

# Why not `System.JavaScript`?

Bootsharp itself is built on top of [.NET's new JavaScript interop](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/import-export-interop?view=aspnetcore-8.0) introduced in .NET 7.

If you're looking to expose simple library API to JavaScript and don't need type declarations, Bootsharp would probably be an overkill. However, .NET's interop is low-level, doesn't support passing custom types by value and requires lots of boilerplate to author the bindings. It's impractical for large API surfaces.

With Bootsharp, you'll be able to just throw your domain-specific interfaces at it and use them seamlessly from the other side, as if they were originally authored in TypeScrip (and vice-versa).

# 🎬 Get Started

https://bootsharp.com/guide/getting-started
