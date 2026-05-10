# Build Configuration

Build and publish related options are configured in `.csproj` file via MSBuild properties.

| Property                   | Default          | Description                                       |
|----------------------------|------------------|---------------------------------------------------|
| BootsharpName              | bootsharp        | Name of the generated JavaScript module.          |
| BootsharpPublishDirectory  | /bin/bootsharp   | Directory to publish generated JavaScript module. |
| BootsharpBinariesDirectory | publish-dir/bin  | Directory to publish binaries.                    |
| BootsharpPackageDirectory  | project-dir      | Directory to publish `package.json` file.         |

Below is an example configuration, which will make Bootsharp name the compiled module "backend" (instead of the default "bootsharp"), publish the `package.json` under the solution directory root and emit the runtime binaries into a "public/bin" directory one level above the solution root:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
        <BootsharpName>backend</BootsharpName>
        <BootsharpPackageDirectory>$(SolutionDir)</BootsharpPackageDirectory>
        <BootsharpBinariesDirectory>$(SolutionDir)../public/bin</BootsharpBinariesDirectory>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Bootsharp" Version="*-*"/>
    </ItemGroup>

</Project>
```

## Globalization

By default, Bootsharp disables .NET globalization on WASM. This keeps the published output smaller, but culture-specific formatting and culture construction will use invariant mode.

To enable globalization, explicitly disable invariant globalization in your project file:

```xml
<PropertyGroup>
    <InvariantGlobalization>false</InvariantGlobalization>
</PropertyGroup>
```

When invariant globalization is disabled, Bootsharp will automatically include the ICU files emitted by the .NET WASM build and configure the runtime accordingly.

Bootsharp supports the following globalization modes:

| Mode    | How to enable                                                        | Behavior                                                                                  |
|---------|----------------------------------------------------------------------|-------------------------------------------------------------------------------------------|
| Sharded | Didable `InvariantGlobalization`                                     | Publishes the default sharded ICU files (`icudt_*.dat`).                                  |
| Full    | Didable `InvariantGlobalization` and enable `WasmIncludeFullIcuData` | Publishes the full ICU data file (`icudt.dat`) and supports many cultures in one runtime. |
