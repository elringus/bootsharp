# LLVM-Native Interop Migration Plan

## Goal

Replace `System.Runtime.InteropServices.JavaScript` (`[JSImport]`/`[JSExport]`) with raw NativeAOT-LLVM C-ABI bindings (`[UnmanagedCallersOnly]` + `[DllImport]` + Emscripten js-library). Replace runtime-bound wiring (`runtime.setModuleImports`, `runtime.getAssemblyExports`) with link-time wiring (Emscripten `--js-library` + direct `Module._fn` calls). Drop Mono support entirely.

## Performance

The interop layer is the single hottest piece of code in Bootsharp. Maximum performance is the primary design constraint, not a "nice to have". No compromises.

## Progress to date

- Implmeneted **Async bridge** in the previous commit (0d69720) — built on top of existing sync `[JSImport]`/`[JSExport]`. `Task` / `Task<T>` are no longer marshaled by .NET's runtime; every async method goes through generator-emitted `_Notify`/`_Fail`/`_Complete` callbacks paired with `Bootsharp.PendingImports` (C# slot pool) and `src/js/src/tasks.mts` `pendingExports` (JS slot pool). The same callback pattern carries over to raw C-ABI without changes.
- **`BsLlvm` MSBuild flag deleted.** LLVM is always the compiler in both debug and release.

## Validation

Make sure all E2E tests (except explicitely ignored ones) pass after you finish the migration. Code coverage is out of scope for this task.

## Files to read first (in this order)

1. [samples/bench/dotnet-llvm/Program.cs](samples/bench/dotnet-llvm/Program.cs) + [imports.js](samples/bench/dotnet-llvm/imports.js) + [DotNetLLVM.csproj](samples/bench/dotnet-llvm/DotNetLLVM.csproj) + [init.mjs](samples/bench/dotnet-llvm/init.mjs) — the target architecture, end-to-end.
2. [InteropGenerator.cs](src/cs/Bootsharp.Publish/GenerateCS/InteropGenerator.cs) — main C# emitter. Async-bridge split is already in place; the attribute layer is what changes.
3. [InstanceGenerator.cs](src/cs/Bootsharp.Publish/GenerateCS/InstanceGenerator.cs) — instance proxy emission.
4. [JSModuleGenerator.cs](src/cs/Bootsharp.Publish/GenerateJS/JSModuleGenerator.cs) — main JS emitter. Async-bridge shape already in place.
5. [JSImportsGenerator.cs](src/cs/Bootsharp.Publish/GenerateJS/JSImportsGenerator.cs) — currently emits the `setModuleImports` wiring; becomes the Emscripten `mergeInto(LibraryManager.library, …)` emitter.
6. [Bootsharp.props](src/cs/Bootsharp/Build/Bootsharp.props) + [Bootsharp.targets](src/cs/Bootsharp/Build/Bootsharp.targets) — build pipeline.
7. [boot.mts](src/js/src/boot.mts) + [imports.mts](src/js/src/imports.mts) + [exports.mts](src/js/src/exports.mts) + [runtime.mts](src/js/src/runtime.mts) — JS runtime layer.
