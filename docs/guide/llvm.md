# NativeAOT-LLVM

Starting with v0.6.0 Bootsharp supports .NET's experimental [NativeAOT-LLVM](https://github.com/dotnet/runtimelab/tree/feature/NativeAOT-LLVM) backend.

By default, when targeting `browser-wasm`, .NET is using the Mono runtime, even when compiled in AOT mode. Compared to the modern NativeAOT (previously CoreRT) runtime, Mono's performance is lacking in speed, binary size and compilation times. NativeAOT-LLVM backend not only uses the modern runtime instead of Mono, but also optimizes it with the [LLVM](https://llvm.org) toolchain, further improving the performance.

Below is a benchmark comparing interop and compute performance of various languages and .NET versions compiled to WASM to give you a rough idea on the differences:

![](/img/llvm-bench.png)

— sources of the benchmark are here: https://github.com/elringus/bootsharp/tree/main/samples/bench.

## Setup

Starting with Bootsharp 0.8.0 no extra project configuration is required.

When publishing a Bootsharp project in `Release`, Bootsharp automatically enables the NativeAOT-LLVM toolchain, speed-focused code generation, and the trimming settings required by the LLVM backend.

## Binaryen

Bootsharp always tries to run Binaryen on release publishes with speed optimization enabled:

1. Install Binaryen: https://github.com/WebAssembly/binaryen/releases
2. Make sure `wasm-opt` is in the system path
3. If the tool is missing, Bootsharp will log a warning and continue with a non-fully-optimized WASM binary
