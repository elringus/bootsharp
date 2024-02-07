# Sideloading Binaries

By default, Bootsharp build task will embed project's DLLs and .NET WASM runtime to the generated JavaScript module. While convenient and even required in some cases (eg, for VS Code web extensions), this also adds about 30% of extra size due to binary -> base64 conversion of the embedded files.

To disable the embedding, set `BootsharpEmbedBinaries` build property to false:

```xml

<PropertyGroup>
    <BootsharpEmbedBinaries>false</BootsharpEmbedBinaries>
</PropertyGroup>
```

The `dotnet.wasm` and solution's assemblies will be emitted in the build output directory. You will then have to provide them when booting:

```ts
const resources = {
    wasm: Uint8Array,
    assemblies: [{ name: "Foo.wasm", content: Uint8Array }],
    entryAssemblyName: "Foo.dll"
};
await dotnet.boot({ resources });
```

— this way the binary files can be streamed directly from server to optimize traffic and initial load time.

Alternatively, set `root` property of the boot options and Bootsharp will automatically fetch the resources form the specified URL:

```ts
// Assuming the resources are stored in "bin" directory under website root.
await backend.boot({ root: "/bin" });
```

::: tip EXAMPLE
Find sideloading example in the [React sample](https://github.com/elringus/bootsharp/blob/main/samples/react).
:::
