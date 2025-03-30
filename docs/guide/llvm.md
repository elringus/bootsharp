# NativeAOT-LLVM

Starting with v0.6.0 Bootsharp supports .NET's experimental [NativeAOT-LLVM](https://github.com/dotnet/runtimelab/tree/feature/NativeAOT-LLVM) backend.

By default, when targeting `browser-wasm`, .NET is using the Mono runtime, even when compiled in AOT mode. Compared to the modern NativeAOT (previously CoreRT) runtime, Mono's performance is lacking in speed, binary size and compilation times. NativeAOT-LLVM backend not only uses the modern runtime instead of Mono, but also optimizes it with the [LLVM](https://llvm.org) toolchain, further improving the performance.

Below is a benchmark comparing interop and compute performance of various languages and .NET versions compiled to WASM to give you a rough idea on the differences:

|             | Rust  | .NET LLVM | .NET AOT  | Go      |
|-------------|-------|-----------|-----------|---------|
| Echo Number | `1.0` | `11.9`    | `21.1`    | `718.7` |
| Echo Struct | `1.0` | `1.6`     | `4.3`     | `20.8`  |
| Fibonacci   | `1.0` | `1.1`     | `1.5`     | `3.8`   |

— the results are relative to the Rust baseline (lower is better). Sources of the benchmark are here: https://github.com/elringus/bootsharp/tree/main/samples/bench.

## Setup

Use following `.csproj` as a reference for enabling NativeAOT-LLVM with Bootsharp:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <!-- Notice '-browser' postfix. -->
        <TargetFramework>net9.0-browser</TargetFramework>
        <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
        <!-- Let Bootsharp know you're using the LLVM backend. -->
        <BootsharpLLVM>true</BootsharpLLVM>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Bootsharp" Version="*-*"/>
    </ItemGroup>

    <!-- Below are properties required to enable LLVM backend. -->
    <!-- Due to experimental nature of the project, specifics may change over time. -->

    <PropertyGroup>
        <PublishTrimmed>true</PublishTrimmed>
        <DotNetJsApi>true</DotNetJsApi>
        <DebugType>none</DebugType>
        <EmccFlags>$(EmccFlags) -O3</EmccFlags> <!-- optimize speed; use -Oz for min. size -->
        <UsingBrowserRuntimeWorkload>false</UsingBrowserRuntimeWorkload>
        <RestoreAdditionalProjectSources>
            https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/index.json;
        </RestoreAdditionalProjectSources>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.DotNet.ILCompiler.LLVM" Version="10.0.0-*"/>
        <PackageReference Condition="'$([MSBuild]::IsOSPlatform(&quot;Windows&quot;))' == 'true'" Include="runtime.win-x64.Microsoft.DotNet.ILCompiler.LLVM" Version="10.0.0-*"/>
        <PackageReference Condition="'$([MSBuild]::IsOSPlatform(&quot;Windows&quot;))' == 'false'" Include="runtime.linux-x64.Microsoft.DotNet.ILCompiler.LLVM" Version="10.0.0-*"/>
        <EmscriptenEnvVars Include="DOTNET_EMSCRIPTEN_LLVM_ROOT=$(EmscriptenUpstreamBinPath)"/>
        <EmscriptenEnvVars Include="DOTNET_EMSCRIPTEN_BINARYEN_ROOT=$(EmscriptenSdkToolsPath)"/>
        <EmscriptenEnvVars Include="DOTNET_EMSCRIPTEN_NODE_JS=$(EmscriptenNodeBinPath)node$(ExecutableExtensionName)"/>
        <EmscriptenEnvVars Include="EM_CACHE=$(EmscriptenCacheSdkCacheDir)"/>
    </ItemGroup>

</Project>
```
