# Sideloading Binaries

By default, Bootsharp embeds the .NET WASM runtime and the solution's assemblies into the generated JavaScript module as base64 strings. This is convenient — `bootsharp.boot()` works with no arguments and no extra files to serve — and required in some environments (eg, VS Code web extensions). The trade-off is roughly 30% extra bundle size due to the base64 encoding.

To disable embedding, set `BootsharpBinariesDirectory` to the directory where the binaries should be published:

```xml
<PropertyGroup>
    <BootsharpBinariesDirectory>bin/$(BootsharpName)/bin</BootsharpBinariesDirectory>
</PropertyGroup>
```

The `dotnet.native.wasm`, solution assemblies, ICU data and (in debug builds) debug symbols will be emitted to that directory as separate files instead of being inlined into the module. You then have two ways to feed them to `boot`:

Pass a root URL to fetch the resources from at runtime:

```ts
// Assuming the binaries are served from "/bin" under the website root.
await bootsharp.boot("/bin");
```

Or load the binaries yourself and pass them as a `BootResources` object:

```ts
import { readFileSync } from "node:fs";

const wasm = readFileSync("bin/dotnet.native.wasm");
await bootsharp.boot({ wasm });
```

This way the binary files can be streamed directly from the server, cached separately, or loaded from any source you control — useful for trimming initial bundle size or sharing the runtime across multiple modules.

::: tip EXAMPLE
Find sideloading example in the [minimal sample](https://github.com/elringus/bootsharp/blob/main/samples/minimal).
:::
