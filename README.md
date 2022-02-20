# DotNetJS

[![NuGet](https://img.shields.io/nuget/v/DotNetJS)](https://www.nuget.org/packages/DotNetJS)
[![npm](https://img.shields.io/npm/v/dotnet-runtime)](https://www.npmjs.com/package/dotnet-runtime)
[![CodeFactor](https://codefactor.io/repository/github/elringus/dotnetjs/badge/main)](https://codefactor.io/repository/github/elringus/dotnetjs/overview/main)
[![codecov](https://codecov.io/gh/Elringus/DotNetJS/branch/main/graph/badge.svg?token=AAhei51ETt)](https://codecov.io/gh/Elringus/DotNetJS)
[![CodeQL](https://github.com/Elringus/DotNetJS/actions/workflows/codeql.yml/badge.svg)](https://github.com/Elringus/DotNetJS/actions/workflows/codeql.yml)

The solution provides user-friendly workflow for consuming .NET C# programs and libraries in any JavaScript environment, be it web browsers, Node.js or custom restricted spaces, like [web extensions](https://code.visualstudio.com/api/extension-guides/web-extensions) for VS Code, where neither node modules nor browser APIs are available.

![](https://i.gyazo.com/920943de6864d967b10d30eec261fa1c.png)

The solution is based on two main components:

- [JavaScript/dotnet-runtime](https://github.com/Elringus/DotNetJS/tree/main/JavaScript/dotnet-runtime) ([npm](https://www.npmjs.com/package/dotnet-runtime)). Consumes compiled C# assemblies and .NET runtime WebAssembly module to provide C# interoperability layer in JavaScript. The library is environment-agnostic — it doesn't depend on platform-specific APIs, like browser DOM or node modules and can be imported as CommonJS or ECMAScript module or consumed via script tag in browsers.
- [DotNet/DotNetJS](https://github.com/Elringus/DotNetJS/tree/main/DotNet/DotNetJS) ([NuGet](https://www.nuget.org/packages/DotNetJS)). Provides JavaScript interoperability layer in C# and packs project output into single-file JavaScript library via MSBuild task. Produced library contains dotnet-runtime initialized with the project assemblies and ready to be used as interoperability layer for the packaged C# project. Can optionally emit type definitions to bootstrap TypeScript development.

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

partial class Program
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
    dotnet.HelloWorld.GetHostName = () => "Browser";

    window.onload = async function () {
        // Booting the DotNet runtime and invoking entry point.
        await dotnet.boot();
        // Invoking 'GetName()' C# method defined in 'HelloWorld' assembly.
        const guestName = dotnet.HelloWorld.GetName();
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
dotnet.HelloWorld.GetHostName = () => "Node.js";

(async function () {
    // Booting the DotNet runtime and invoking entry point.
    await dotnet.boot();
    // Invoking 'GetName()' C# method defined in 'HelloWorld' assembly.
    const guestName = dotnet.HelloWorld.GetName();
    console.log(`Welcome, ${guestName}! Enjoy your module space.`);
})();
```

## Example Projects

Find the following sample projects in this repository:

- [Hello World](https://github.com/Elringus/DotNetJS/tree/main/Samples/HelloWorld) — Consume the produced library as a global import in browser, CommonJS or ES module in node.
- [Web Extension](https://github.com/Elringus/DotNetJS/tree/main/Samples/WebExtension) — Consume the library in VS Code web extension, which works in both web and standalone versions of the IDE.
- [Runtime Tests](https://github.com/Elringus/DotNetJS/tree/main/JavaScript/dotnet-runtime/test) — Integration tests featuring various usage scenarios: async method invocations, interop with instances, sending raw byte arrays, streaming, etc.

## Sideloading Binaries

By default, DotNetJS build task will embed project's DLLs and .NET WASM runtime to the generated JS library. While convenient and even required in some cases (eg, for VS Code web extensions), this also adds about 30% of extra size due to binary->base64 conversion of the embedded files.

To disable the embedding, set `EmbedBinaries` build property to false. You will then have to provide the required data when booting `dotnet.js`:

```js
const bootData = {
    wasm: {},
    assemblies: [],
    entryAssemblyName: "Project.dll"
};
await dotnet.boot(bootData);
```

— this way the binary files can be streamed directly from the server to optimize traffic and initial load time.

When embedding is disabled, you will probably want to preserve build artifacts as well. Set `Clean` build property to false to prevent DotNetJS from wiping them:

```xml
<PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <EmbedBinaries>false</EmbedBinaries>
    <Clean>false</Clean>
</PropertyGroup>
```

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

To compile and test the runtime run the following in order (under [dotnet-runtime](https://github.com/Elringus/DotNetJS/tree/main/JavaScript/dotnet-runtime) folder):

```
scripts/install-emsdk.sh
scripts/compile-runtime.sh
npm build
scripts/compile-test.sh
npm test
```
