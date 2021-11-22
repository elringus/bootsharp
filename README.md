# DotNetJS

[![NuGet](https://img.shields.io/nuget/v/DotNetJS)](https://www.nuget.org/packages/DotNetJS)
[![npm](https://img.shields.io/npm/v/dotnet-runtime)](https://www.npmjs.com/package/dotnet-runtime)
[![CodeFactor](https://codefactor.io/repository/github/elringus/dotnetjs/badge/main)](https://codefactor.io/repository/github/elringus/dotnetjs/overview/main)
[![codecov](https://codecov.io/gh/Elringus/DotNetJS/branch/main/graph/badge.svg?token=AAhei51ETt)](https://codecov.io/gh/Elringus/DotNetJS)
[![CodeQL](https://github.com/Elringus/DotNetJS/actions/workflows/codeql.yml/badge.svg)](https://github.com/Elringus/DotNetJS/actions/workflows/codeql.yml)

This project is dedicated to providing user-friendly workflow for consuming .NET C# programs and libraries in any JavaScript environment, be it web browsers, Node.js or custom restricted spaces, like [web extensions](https://code.visualstudio.com/api/extension-guides/web-extensions) for VS Code.

The solution is based on two main components:

 - JavaScript library — [dotnet-runtime](https://www.npmjs.com/package/dotnet-runtime). Consumes compiled C# assemblies and .NET runtime WebAssembly module to provide C# interoperability layer in JavaScript. The library is environment-agnostic — it doesn't depend on platform-specific APIs, like browser DOM or node modules and can be consumed as CommonJS or ECMAScript module or imported via script tag in browsers.
 - NuGet C# package — [DotNetJS](https://www.nuget.org/packages/DotNetJS). Provides JavaScript interoperability layer in C# and packs project output into single-file JavaScript library via MSBuild task. The packed library uses embeded assemblies to initialize dotnet-runtime library. Can optionally emit declarations and type definions to bootstrap the interoperability.

## Quick Start

In C# project configuration file specify `Microsoft.NET.Sdk.BlazorWebAssembly` SDK and import `DotNetJS` NuGet package:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

    <PropertyGroup>
        <TargetFramework>net6.0</TargetFramework>
        <LangVersion>10</LangVersion>
    </PropertyGroup>

    <ItemGroup>
        <!-- Make sure to specify the latest available version. -->
        <PackageReference Include="DotNetJS" Version="0.2.5"/>
    </ItemGroup>

</Project>
```

To invoke a JavaScript function in C# use `JS.Invoke(functionName, args)` method. To expose a C# method to JavaScript, use `[JSInvokable]` attribute:

```csharp
using System;
using DotNetJS;
using Microsoft.JSInterop;

// Entry assembly name defines generated JavaScript file and
// main export object names. Can be changed in build configuration.
namespace HelloWorld;

class Program
{
    // Entry point is invoked by the JavaScript runtime on boot.
    void Main ()
    {
        // Invoking 'getName()' JavaScript function.
        var hostName = JS.Invoke<string>("getName");
        // Writing to JavaScript host console.
        Console.WriteLine($"Hello {hostName}, DotNet here!");
    }

    [JSInvokable] // The method is invoked from JavaScript.
    public static string GetName () => "DotNet";
}
```

Publish the project with `dotnet publish`. A single-file JavaScript library will be produced at the "bin" directory. Consume the library depending on the environment:

### Browser

```html
<!-- Import as a global 'HelloWorld' object via script tag. -->
<script src="HelloWorld.js"></script>

<script>
    
    // This function is invoked from C#.
    window.getName = () => "Browser";
    
    window.onload = async function () {
        // Booting the DotNet runtime and invoking entry point.
        await HelloWorld.boot();
        // Invoking 'GetName()' C# method.
        const guestName = HelloWorld.invoke("GetName");
        console.log(`Welcome, ${guestName}! Enjoy your global space.`);
    };
    
</script>
```

### Node.js

```js
// Import as CommonJS module.
const HelloWorld = require("HelloWorld");
// ... or as ECMAScript module in node v17 or later.
import HelloWorld from "HelloWorld.js";

// This function is invoked from C#.
global.getName = () => "Node.js";

(async function () {
    // Booting the DotNet runtime and invoking entry point.
    await HelloWorld.boot();
    // Invoking 'GetName()' C# method.
    const guestName = HelloWorld.invoke("GetName");
    console.log(`Welcome, ${guestName}! Enjoy your module space.`);
})();
```

## Example Projects

Find the following sample projects in this repository:

 - [Hello World](https://github.com/Elringus/DotNetJS/tree/main/Examples/HelloWorld) — Consume the produced library as a global import in browser, CommonJS or ES module in node.
 - [Web Extension](https://github.com/Elringus/DotNetJS/tree/main/Examples/WebExtension) — Consume the library in VS Code web extension, which works in both web and standalone versions of the IDE.
 - [Runtime Tests](https://github.com/Elringus/DotNetJS/tree/main/Runtime/test) — Integration tests featuring various usage scenarios: async method invocations, interop with instances, sending raw byte arrays, streaming, etc.

## Build Properties

Specify following optional properties in .csproj to customize the build:

 - `<Clean>false<Clean>` — do not clean the build output folders.
 - `<LibraryName>CustomName</LibraryName>` — specify a custom name for the generated library file and export object.

For example, following configuration will preserve the build artifacts and produce `my-dotnet-lib.js` library with `my-dotnet-lib` export object:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

    <PropertyGroup>
        <TargetFramework>net6.0</TargetFramework>
        <LangVersion>10</LangVersion>
        <Clean>false</Clean>
        <LibraryName>my-dotnet-lib</LibraryName>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="DotNetJS" Version="0.1.0"/>
    </ItemGroup>

</Project>
```

## Compiling Runtime

To compile and test the runtime run the following in order (under [Runtime](https://github.com/Elringus/DotNetJS/tree/main/Runtime) folder):

```
scripts/install-emsdk.sh
scripts/compile-runtime.sh
scripts/compile-test.sh
npm build
npm test
```

## Publishing Runtime

A memo for the publishing process after modifying JS runtime.

1. Bump NPM version on `./Runtime/package.json` and:
 - `npm run build`
 - `scripts/publish-package.sh`
2. Bump NuGet version on `./DotNetJS/DotNetJS.csproj` and:
 - `dotnet pack -c Release --output bin`
 - `dotnet nuget push bin/DotNetJS.{VER}.nupkg --api-key {KEY} --source https://api.nuget.org/v3/index.json`
3. Wait for the package indexing, bump NuGet version on `./Runtime/test/Test.csproj` and:
 - `script/compile-test.sh`
4. Remind myself that this should be automated.
