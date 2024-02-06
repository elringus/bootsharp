# Sideloading Binaries

By default, DotNetJS build task will embed project's DLLs and .NET WASM runtime to the generated JS library. While convenient and even required in some cases (eg, for VS Code web extensions), this also adds about 30% of extra size due to binary->base64 conversion of the embedded files.

To disable the embedding, set `EmbedBinaries` build property to false:

```xml

<PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <EmbedBinaries>false</EmbedBinaries>
</PropertyGroup>
```

The `dotnet.wasm` and solution's assemblies will be emitted in the build output directory. You will then have to provide them when booting:

```js
const bootData = {
    wasm: Uint8Array,
    assemblies: [{ name: "Foo.dll", data: Uint8Array }],
    entryAssemblyName: "Foo.dll"
};
await dotnet.boot(bootData);
```

— this way the binary files can be streamed directly from server to optimize traffic and initial load time.

Use `getBootUris()` function to get identifiers of all the resources required for boot. Below is an example on fetching the boot data; it assumes both wasm and assemblies are stored under `/bin` directory on the remote server:

```js
async function fetchBootData() {
    const uris = getBootUris();
    return {
        wasm: await fetchBinary(uris.wasm),
        assemblies: await Promise.all(uris.assemblies.map(fetchAssembly)),
        entryAssemblyName: uris.entryAssembly
    };

    async function fetchBinary(name: string) {
        const uri = `${process.env.PUBLIC_URL}/bin/${name}`;
        return new Uint8Array(await (await fetch(uri)).arrayBuffer());
    }

    async function fetchAssembly(name: string) {
        return { name, data: await fetchBinary(name) };
    }
}
```

Find sideloading example in the [React sample](https://github.com/Elringus/DotNetJS/blob/main/Samples/React/src/boot.ts). Also, take a look at the [build script](https://github.com/Elringus/DotNetJS/blob/main/Samples/React/scripts/build-backend.sh), which automatically deploys the binaries to the react public directory after building the backend.
