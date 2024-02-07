# Dependency Injection

When using [interop interfaces](/guide/interop-interfaces), it's convenient to use a dependency injection mechanism to automatically route generated interop implementations for the services that needs them.

Reference `Bootsharp.Inject` extension in the project configuration:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Bootsharp" Version="*-*"/>
        <PackageReference Include="Bootsharp.Inject" Version="*-*"/>
        <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="*"/>
    </ItemGroup>

</Project>
```

— and use `AddBootsharp` extension method to inject the generated import implementations; `RunBootsharp` will initialize generated export implementation by requiring the handlers, which should be added to the services collection before.

```csharp
using Bootsharp;
using Bootsharp.Inject;
using Microsoft.Extensions.DependencyInjection;

[assembly: JSExport(
    typeof(IExported)
)]

[assembly: JSImport(
    typeof(IImported)
)]

new ServiceCollection()
    // Inject generated implementation of IImported.
    .AddBootsharp()
    // Inject other services, which may require IImported.
    .AddSingleton<SomeService>()
    // Provide handler for the exported interface.
    .AddSingleton<IExported, Exported>()
    .BuildServiceProvider()
    // Initialize the exported implementations.
    .RunBootsharp();
```

`IImported` can now be requested via .NET's DI, while `IExported` APIs are available in JavaScript:

```csharp
public class SomeService (IImported imported) { }
```

```ts
import { Exported } from "bootsharp";
```
