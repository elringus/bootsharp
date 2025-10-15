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

Bootsharp streamlines the integration of .NET C# apps and libraries into web projects. It's ideal for building applications where the domain (backend) is authored in .NET C#, while the UI (frontend) is a standalone TypeScript or JavaScript project. Think of it as [Embind](https://emscripten.org/docs/porting/connecting_cpp_and_javascript/embind.html) for C++ or [wasm-bindgen](https://github.com/rustwasm/wasm-bindgen) for Rust.

![](https://raw.githubusercontent.com/elringus/bootsharp/main/docs/public/img/banner.png)

Facilitating high-level interoperation between C# and TypeScript, Bootsharp lets you build the UI layer within its natural ecosystem using industry-standard tooling and frameworks, such as [React](https://react.dev) and [Svelte](https://svelte.dev). The project can then be published to the web or bundled as a native desktop or mobile application with [Electron](https://electronjs.org) or [Tauri](https://tauri.app).

## Features

✨ High-level C# <-> TypeScript interop

📦 Embeds binaries to single-file ES module

🗺️ Works in browsers and JS runtimes (Node, Deno, Bun)

⚡ Generates bindings and types over C# interfaces

🏷️ Supports interop over object instances

🛠️ Allows customizing emitted bindings

🔥 Supports multi-threading, NativeAOT-LLVM, trimming

## 🎬 Get Started

https://bootsharp.com/guide

---

<a href="https://naninovel.com">
  <p align="center">Bootsharp is used in <strong>Naninovel: Visual Novel, Dialogue & Cutscene Storytelling Engine</strong>. Check it out!</p>
  <p align="center"><img alt="naninovel banner" src="https://raw.githubusercontent.com/elringus/cdn/main/naninovel-banner-wide.png"></p>
</a>
