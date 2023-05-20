# DotNetJS

[![NuGet](https://img.shields.io/nuget/v/DotNetJS)](https://www.nuget.org/packages/DotNetJS)
[![CodeFactor](https://codefactor.io/repository/github/elringus/dotnetjs/badge/main)](https://codefactor.io/repository/github/elringus/dotnetjs/overview/main)
[![codecov](https://codecov.io/gh/Elringus/DotNetJS/branch/main/graph/badge.svg?token=AAhei51ETt)](https://codecov.io/gh/Elringus/DotNetJS)
[![CodeQL](https://github.com/Elringus/DotNetJS/actions/workflows/codeql.yml/badge.svg)](https://github.com/Elringus/DotNetJS/actions/workflows/codeql.yml)

The solution provides user-friendly workflow for consuming .NET C# programs and libraries in any JavaScript environment, be it web browsers, Node.js or custom restricted spaces, like [web extensions](https://code.visualstudio.com/api/extension-guides/web-extensions) for VS Code, where neither node modules nor browser APIs are available.

![](https://raw.githubusercontent.com/Elringus/CDN/main/dotnetjs-2.png)

The solution is based on two main components:

- [DotNet](https://github.com/Elringus/DotNetJS/tree/main/DotNet). Provides JavaScript interoperability layer in C# and packs project output into single-file JavaScript library via MSBuild task. Produced library contains dotnet runtime initialized with the project assemblies and ready to be used as interoperability layer for the packaged C# project. Can optionally emit type definitions to bootstrap TypeScript development.
- [JavaScript](https://github.com/Elringus/DotNetJS/tree/main/JavaScript). Consumes compiled C# assemblies and .NET runtime WebAssembly module to provide C# interoperability layer in JavaScript. The library is environment-agnostic — it doesn't depend on platform-specific APIs, like browser DOM or node modules and can be imported as CommonJS or ECMAScript module or consumed via script tag in browsers.

For example, below is all you need to get fully-typed interop layer between a language server written in C# and VS Code extension that runs in node and web browsers:

```csharp
[assembly: JSExport(new[] {
    typeof(ICompletionHandler),
    typeof(IHoverHandler),
    // ... other C# API to expose to JS
})]

[assembly: JSImport(new[] {
    typeof(IDiagnosticPublisher)
    // ... other JS API to expose to C#
})]

public static class Program
{
    public static void Main () => new ServiceCollection()
        .AddSingleton<IDocumentRegistry, DocumentRegistry>()
        .AddSingleton<ISettingsHandler, SettingsHandler>()
        // ... other application services
        .AddJS() // injects auto-generated services specified above
        .BuildServiceProvider();
}
```

— find full sample here: [Naninovel.Language](https://github.com/Naninovel/Language).

## Important Considerations

### Interoperability

.NET and JavaScript are completely different runtime environments; don't expect native-like communication with arbitrary data types. While it's possible to transfer primitives (number, boolean, string, array) as-is, more complex types have to be marshalled with JSON.

If you're looking to bring existing .NET library to JS, you'd most likely have to implement an interop layer with JSON-serializable data structures. Instances, events, delegates, dynamic types and other runtime-specific features can't be used/passed directly to JS.

### Breaking Changes

.NET's WASM support is constantly evolving and this project is in early development stage as well. Expect major breaking changes until the first stable release: https://github.com/Elringus/DotNetJS/milestone/1. When (or if) this happen depends on the community interest in the project. Assistance with issues labeled "help wanted" is most appreciated.

### .NET Support

We are using custom .NET 6 branch due to compatibility issues in the stock runtime. Even if you specify .NET 7 or newer in project manifest, the code will still run on .NET 6 when built for WASM. We have plans to migrate to stock .NET runtime once the issues are resolved (https://github.com/Elringus/DotNetJS/issues/20).

## Quick Start

In C# project configuration file specify `Microsoft.NET.Sdk.BlazorWebAssembly` SDK and import `DotNetJS` NuGet package:

```xml

<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

    <PropertyGroup>
        <TargetFramework>net6.0</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="DotNetJS" Version="*"/>
    </ItemGroup>

</Project>
```

To associate a JavaScript function with a C# method use `JSFunction` attribute. To expose a C# method to JavaScript, use `JSInvokable` attribute:

```csharp
using System;
using DotNetJS;
using Microsoft.JSInterop;

namespace HelloWorld;

public partial class Program
{
    // Entry point is invoked by the JavaScript runtime on boot.
    public static void Main ()
    {
        // Invoking 'dotnet.HelloWorld.GetHostName()' JavaScript function.
        var hostName = GetHostName();
        // Writing to JavaScript host console.
        Console.WriteLine($"Hello {hostName}, DotNet here!");
    }

    [JSFunction] // The interoperability code is auto-generated.
    public static partial string GetHostName ();

    [JSInvokable] // The method is invoked from JavaScript.
    public static string GetName () => "DotNet";
}
```

Publish the project with `dotnet publish`. A single-file `dotnet.js` library will be produced under the "bin" directory. Consume the library depending on the environment:

### Browser

```html
<!-- Import as a global 'dotnet' object via script tag. -->
<script src="dotnet.js"></script>

<script>

    // Providing implementation for 'GetHostName' function declared in 'HelloWorld' C# assembly.
    dotnet.HelloWorld.getHostName = () => "Browser";

    window.onload = async function () {
        // Booting the DotNet runtime and invoking entry point.
        await dotnet.boot();
        // Invoking 'GetName()' C# method defined in 'HelloWorld' assembly.
        const guestName = dotnet.HelloWorld.getName();
        console.log(`Welcome, ${guestName}! Enjoy your global space.`);
    };

</script>
```

### Node.js

```js
// Import as CommonJS module.
const dotnet = require("dotnet");
// ... or as ECMAScript module in node v17 or later.
import dotnet from "dotnet.js";

// Providing implementation for 'GetHostName' function declared in 'HelloWorld' C# assembly.
dotnet.HelloWorld.getHostName = () => "Node.js";

(async function () {
    // Booting the DotNet runtime and invoking entry point.
    await dotnet.boot();
    // Invoking 'GetName()' C# method defined in 'HelloWorld' assembly.
    const guestName = dotnet.HelloWorld.getName();
    console.log(`Welcome, ${guestName}! Enjoy your module space.`);
})();
```

## Example Projects

Find the following sample projects in this repository:

- [Hello World](https://github.com/Elringus/DotNetJS/tree/main/Samples/HelloWorld) — Consume the produced library as a global import in browser, CommonJS or ES module in node.
- [Web Extension](https://github.com/Elringus/DotNetJS/tree/main/Samples/WebExtension) — Consume the library in VS Code web extension, which works in both web and standalone versions of the IDE.
- [React](https://github.com/Elringus/DotNetJS/tree/main/Samples/React) — A sample React app, which uses dotnet as backend. Features binaries side-loading and mocking dotnet APIs in unit tests.
- [Runtime Tests](https://github.com/Elringus/DotNetJS/tree/main/JavaScript/test) — Integration tests featuring various usage scenarios: async method invocations, interop with instances, sending raw byte arrays, streaming, etc.

A real-life usage of the solution can be found in https://github.com/Naninovel/Language. The project is an implementation of language server protocol that is used in VS Code extension: https://github.com/Naninovel/VSCode.

## Events

To make a C# method act as event broadcaster for JavaScript consumers, annotate it with `[JSEvent]` attribute:

```csharp
[JSEvent]
public static partial string OnSomethingHappened (string payload);
```

— and consume it from JavaScript as follows:

```js
dotnet.MyApp.onSomethingHappened.subscribe(handleSomething);
dotnet.MyApp.onSomethingHappened.unsubscribe(handleSomething);

function handleSomething(payload) {

}
```

When the method in invoked in C#, subscribed JavaScript handlers will be notified.

In TypeScript the event will have typed generic declaration corresponding to the event arguments.

### React Event Hooks

Below are some utility hooks, which you can use in React to bootstrap usage of the events:

```ts
import { DependencyList, useEffect, useState } from "react";
import { EventSubscriber } from "backend";

export function useEvent<T extends any[]>(
    event: EventSubscriber<T>, handler: (...args: [...T]) => void,
    deps?: DependencyList | undefined, destructor?: () => void) {
    useEffect(() => {
        event.subscribe(handler);
        return () => {
            event.unsubscribe(handler);
            destructor?.();
        };
    }, deps);
}

export function useEventState<T extends any[]>(
    event: EventSubscriber<T>,
    defaultState?: T[0]): T[0] | undefined {
    const last = event.getLast();
    const initialState = last === undefined ? defaultState : last[0];
    const [state, setState] = useState<T[0]>(initialState);
    useEvent<T[0]>(event, setState, []);
    return state;
}
```

The `useEventState` hook will take care of both subscribing and unsubscribing from the dotnet event when component unmounts and using last event args as the default state to catch up in case the component missed a broadcast before being mounted.

```tsx
const MyComponent = () => {
    const myData = useEventState(Backend.onDataChanged);
    return <>{myData}</>;
};
```

## Auto-generating Bindings by Interfaces

Instead of writing a binding for each method, make DotNetJS generate them automatically with `[JSImport]` and `[JSExport]` assembly attributes.

For example, let's say we have a JS frontend, which needs to be notified when a data is mutated on our C# backend, so it can render the updated state; additionally, our frontend may have a setting (eg, stored in browser cache) to temporary mute notifications, which needs to be retrieved by the backend. Create the following interface in C# to describe the expected frontend APIs:

```csharp
interface IFrontend
{
    void NotifyDataChanged (Data data);
    bool IsMuted ();
}
```

Now add the interface type to the JS import list:

```csharp
[assembly: JSImport(new[] { typeof(IFrontend) })]
```

DotNetJS will generate the following C# implementation:

```csharp
public class JSFrontend : IFrontend
{
    [JSFunction] public static void NotifyDataChanged (Data data) => JS.Invoke("dotnet.Frontend.notifyDataChanged.broadcast", new object[] { data });
    [JSFunction] public static bool IsMuted () => JS.Invoke<bool>("dotnet.Frontend.isMuted");

    void IFrontend.NotifyDataChanged (Data data) => NotifyDataChanged(data);
    bool IFrontend.IsMuted () => IsMuted();

}
```

— which you can use in C# to interop with the frontend and following TypeScript spec to be implemented on the frontend:

```ts
export namespace Frontend {
    export const notifyDataChanged: Event<[Data]>;
    export let isMuted: () => boolean;
}
```

Now let's say we want to provide an API for frontend to request mutation of the data:

```csharp
interface IBackend
{
    void AddData (Data data);
}
```

Export the interface to JS:

```csharp
[assembly: JSExport(new[] { typeof(IBackend) })]
```

Get the following implementation auto-generated:

```csharp
public class JSBackend
{
    private static IBackend handler = null!;

    public JSBackend (IBackend handler)
    {
        JSBackend.handler = handler;
    }

    [JSInvokable] public static void AddData (Data data) => handler.AddData(data);
}
```

— which will as well produce following spec to be consumed on JS side:

```ts
export namespace Backend {
    export function addData(data: Data): void;
}
```

Find example on using the attributes in the [React sample](https://github.com/Elringus/DotNetJS/blob/main/Samples/React).

### Overriding Bindings

Both export and import attributes have additional parameters allowing to override generated binding methods names and invocation bodies.

Let's say we want to rename methods starting with `Notify...` to `On...` on the JS side to make it more clear that we are notifying on C# side and consuming events on JS:

```csharp
[assembly: JSImport(new[] {
    ...
}, namePattern: "Notify(.+)", nameReplacement: "On$1")]
```

Or maybe we want to wrap the exported bindings with some kind of error-catching mechanism:

```csharp
[assembly: JSExport(new[] {
    ...
}, invokePattern: "(.+)", invokeReplacement: "Try(() => $1)")]
```

## Sideloading Binaries

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

## Namespace Pattern

By default, all the generated JavaScript binding objects and TypeScript declarations are grouped under corresponding C# namespaces.

To override the generated namespaces, apply `JSNamespace` attribute to the entry assembly of the C# program. The attribute expects `pattern` and `replacement` arguments, which are provided to [Regex.Replace](https://docs.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex.replace?view=net-6.0#system-text-regularexpressions-regex-replace(system-string-system-string-system-string)) when building the generated namespace name.

For example, to transform `Company.Product.Space` into `Space` namespace, use the following pattern:

```csharp
[assembly:JSNamespace(@"Company\.Product\.(\S+)", "$1")]
```

## JSON Serializer Options

To override default JSON serializer options used for marshalling the interop data, use `JS.Runtime.ConfigureJson` method before the program entry point is invoked. For example, below will add `JsonStringEnumConverter` converter to allow serializing enums via strings:

```csharp
static class Program
{
    static Program () // Static constructor is invoked before 'Main'
    {
        JS.Runtime.ConfigureJson(options =>
            options.Converters.Add(new JsonStringEnumConverter())
        );
    }

    public static void Main () { }
}
```

## Compiling Runtime

To compile and test the runtime run the following in order under [JavaScript](https://github.com/Elringus/DotNetJS/tree/main/JavaScript) folder:

```
scripts/install-emsdk.sh
scripts/compile-runtime.sh
npm build
scripts/compile-test.sh
npm test
```

## FAQ

### I'm getting "An instance of analyzer Generator.SourceGenerator cannot be created" warning

You are probably using an older .NET SDK. Please download the latest version.
