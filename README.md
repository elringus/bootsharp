[![NuGet](https://img.shields.io/nuget/v/DotNetJS)](https://www.nuget.org/packages/DotNetJS)
[![npm](https://img.shields.io/npm/v/dotnet-runtime)](https://www.npmjs.com/package/dotnet-runtime)
[![CodeFactor](https://www.codefactor.io/repository/github/elringus/dotnetjs/badge/main)](https://www.codefactor.io/repository/github/elringus/dotnetjs/overview/main)

This project is dedicated to providing a user-friendly workflow for consuming .NET C# programs and libraries in any JavaScript environments: be it browsers, node.js or custom restricted spaces, like [web extensions](https://code.visualstudio.com/api/extension-guides/web-extensions) for VS Code.

## Quick Start

In C# project (.csproj) specify `Microsoft.NET.Sdk.BlazorWebAssembly` SDK and import DotNetJS NuGet package:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

    <PropertyGroup>
        <TargetFramework>net6.0</TargetFramework>
        <LangVersion>10</LangVersion>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="DotNetJS" Version="0.1.0"/>
    </ItemGroup>

</Project>
```

To invoke a JavaScript function in C# use `JS.Invoke(functionName, args)` method. To expose a C# method to JavaScript, use `[JSInvokable]` attribute:

```csharp
using System;
using DotNetJS;
using Microsoft.JSInterop;

namespace HelloWorld;

public static class Program
{
    public static void Main ()
    {
        var hostName = JS.Invoke<string>("getName");
        Console.WriteLine($"Hello {hostName}, DotNet here!");
    }

    [JSInvokable]
    public static string GetName () => "DotNet";
}
```

Publish the project with `dotnet publish`. A single-file UMD library containing the dotnet runtime and project assemblies will be produced in the "bin" directory. Namespace of the program will be used for both the library file name and main export object. Consume the library depending on the environment:

### Browser

```html
<script src="HelloWorld.js"></script>

<script>
    
    // This function is invoked by DotNet.
    window.getName = () => "Browser";
    
    window.onload = async function () {
        // Booting the DotNet runtime and invoking entry point.
        await HelloWorld.boot();
        // Invoking 'GetName()' method from DotNet.
        const guestName = HelloWorld.invoke("GetName");
        console.log(`Welcome, ${guestName}! Enjoy your global space.`);
    };
    
</script>
```

### Node.js

```js
const HelloWorld = require("HelloWorld");

// This function is invoked by DotNet.
global.getName = () => "Node.js";

(async function () {
    // Booting the DotNet runtime and invoking entry point.
    await HelloWorld.boot();
    // Invoking 'GetName()' method from DotNet.
    const guestName = HelloWorld.invoke("GetName");
    console.log(`Welcome, ${guestName}! Enjoy your CommonJS module space.`);
})();
```
