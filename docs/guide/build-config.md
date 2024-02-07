# Build Configuration

Build and publish related options are configured in `.csproj` file via MSBuild properties.

| Property                    | Default    | Description                                                  |
|-----------------------------|------------|--------------------------------------------------------------|
| BootsharpName               | bootsharp  | Name of the generated JavaScript module.                     |
| BootsharpEmbedBinaries      | true       | Whether to embed binaries to the JavaScript module file.     |
| BootsharpAggressiveTrimming | false      | Whether to disable some .NET features to reduce binary size. |
| BootsharpBundleCommand      | npx rollup | The command to bundle generated JavaScrip solution.          |
| BootsharpPublishDirectory   | /bin       | Directory to publish generated JavaScript module.            |
| BootsharpTypesDirectory     | /types     | Directory to publish type declarations.                      |
| BootsharpBinariesDirectory  | /bin       | Directory to publish binaries when `EmbedBinaries` disabled. |
| BootsharpPackageDirectory   | /          | Directory to publish `package.json` file.                    |

Below is an example configuration, which will make Bootsharp name compiled module "backend" (instead of the default "bootsharp"), publish the module under solution directory root (instead of "/bin"), disable binaries embedding and instead publish them under "public/bin" directory one level above the solution root and enable aggressive assembly trimming to reduce build size:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
        <BootsharpName>backend</BootsharpName>
        <BootsharpPackageDirectory>$(SolutionDir)</BootsharpPackageDirectory>
        <BootsharpEmbedBinaries>false</BootsharpEmbedBinaries>
        <BootsharpBinariesDirectory>$(SolutionDir)../public/bin</BootsharpBinariesDirectory>
        <BootsharpAggressiveTrimming>true</BootsharpAggressiveTrimming>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Bootsharp" Version="*-*"/>
    </ItemGroup>

</Project>
```
