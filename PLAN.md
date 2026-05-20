# Async-Bridge Migration Plan

Replace .NET's built-in async interop marshaling (`[JSMarshalAs<JSType.Promise<…>>] Task<T>`) with a custom callback-based bridge built on top of .NET's existing **sync** `[JSImport]`/`[JSExport]` infrastructure. Delete the `BsLlvm` flag — LLVM is always on. Preserve the debug feature set; LLVM in debug mode compiles without speed optimization for faster iteration.

This document is the handoff for a fresh implementation session. It is self-contained: every file path and decision needed to start is below.

## 1. Goals and non-goals

**Goals**
- Stop using `[JSMarshalAs<JSType.Promise<…>>]` and `Task<T>` returns across the C# ↔ JS boundary.
- Replace the `Promise<T>` round-trip with our own bridge: every async method is a sync `[JSImport]` / `[JSExport]` taskId-passing call, with completion delivered via a paired sync callback in the opposite direction.
- Delete `BsLlvm` MSBuild branching. LLVM is always the compiler.
- Preserve **all** existing debug features:
  - Error message propagation (full message + stack on both sides during dev).
  - Detection of unassigned imported functions when called from C# (the `getImport`/`fooSerializedHandler` indirection in `JSModuleGenerator`).
  - Source maps, debug symbols, the lazy `getExport` resolution path.
- In debug mode, LLVM compiles **without** speed optimization (`-O0` instead of `-O3`) so iteration is faster.

**Non-goals**
- Switching to raw `[UnmanagedCallersOnly]` + `[DllImport("x")]` or Emscripten `--js-library`. That's a much larger follow-up.
- Dropping Mono. Mono branching is removed only insofar as the `BsLlvm` flag is gone; if the Mono publish path still happens to be reachable through .NET defaults, leaving it alone is fine.
- Touching the C# `Bootsharp` library API surface (`Instances`, `JSProxy`, `Modules`, etc.) beyond what the bridge requires.
- Cancellation support across the async bridge (out of scope for v1).
- Performance benchmarking against raw LLVM. The whole point of this scope is that we're keeping .NET's interop infra, so the benchmark uplift comes in a later migration.

## 2. The async bridge

This is the substantive design work. The premise: .NET's runtime can marshal `Task<T>` ↔ `Promise<T>`, but it's the slowest part of the interop layer. We replace just that piece with a sync round-trip + a manual completion callback.

### 2.1 Direction A: JS → C# (export returning `Task<T>`)

**Before** (current generated code, simplified):

```cs
[JSExport]
[return: JSMarshalAs<JSType.Promise<JSType.BigInt>>]
internal static async Task<long> X_Foo (long argsHandle) {
    var args = Deserialize<Args>(argsHandle);
    var result = await User.Foo(args);
    return Serialize(result);
}
```

```js
foo: async (args) => deserialize(await exports.X_Foo(serialize(args, $s.Args)), $s.Result)
```

**After**:

```cs
[JSExport] internal static void X_Foo (int taskId, long argsHandle) {
    _ = Run();
    async global::System.Threading.Tasks.Task Run () {
        try {
            var args = Deserialize<Args>(argsHandle);
            var result = await User.Foo(args);
            X_Foo_Notify(taskId, Serialize(result));
        }
        catch (global::System.Exception e) {
            X_Foo_Fail(taskId, Serialize(e.Message));
        }
    }
}

[JSImport("X.fooNotify", "Bootsharp")] internal static partial void X_Foo_Notify (int taskId, long resultHandle);
[JSImport("X.fooFail", "Bootsharp")]   internal static partial void X_Foo_Fail (int taskId, long messageHandle);
```

```js
// User-facing module:
foo: (args) => new Promise((resolve, reject) => {
    const taskId = pendingExports.allocate(resolve, reject, $s.Result);
    exports.X_Foo(taskId, serialize(args, $s.Args));
})

// Sibling helpers exposed on the Bootsharp module so the C# JSImport binds:
fooNotify (taskId, resultHandle) { pendingExports.complete(taskId, resultHandle); },
fooFail   (taskId, messageHandle) { pendingExports.fail(taskId, messageHandle); }
```

For `Task` (void), the notify takes no result handle: `X_Foo_Notify(taskId)` and `fooNotify(taskId)` resolve with `undefined`.

### 2.2 Direction B: C# → JS (import returning `Task<T>`)

**Before**:

```cs
[JSImport("X.foo", "mod")] [return: JSMarshalAs<JSType.Promise<JSType.BigInt>>]
internal static partial Task<long> X_Foo_Serialized (long argsHandle);
// Wrapped:
public static async Task<X> X_Foo (Args args) => Deserialize<X>(await X_Foo_Serialized(Serialize(args)));
```

```js
foo: async (args) => serialize(await this.fooHandler(deserialize(args, $s.Args)), $s.Result)
```

**After**:

```cs
[JSImport("X.foo", "mod")]
internal static partial void X_Foo_Serialized (int taskId, long argsHandle);

[JSExport] internal static void X_Foo_Complete (int taskId, long resultHandle) =>
    PendingImports.Complete<X>(taskId, resultHandle, &Deserialize<X>);

[JSExport] internal static void X_Foo_Fail (int taskId, long messageHandle) =>
    PendingImports.Fail(taskId, messageHandle);

// Wrapped:
public static global::System.Threading.Tasks.Task<X> X_Foo (Args args) {
    var tcs = new global::System.Threading.Tasks.TaskCompletionSource<X>();
    var id = PendingImports.Allocate(tcs);
    X_Foo_Serialized(id, Serialize(args));
    return tcs.Task;
}
```

```js
// User-facing module:
get foo () { return this.fooHandler; }
set foo (handler) {
    this.fooHandler = handler;
    this.fooSerializedHandler = (taskId, argsHandle) => {
        Promise.resolve()
            .then(() => handler(deserialize(argsHandle, $s.Args)))
            .then(r => exports.X_Foo_Complete(taskId, serialize(r, $s.Result)))
            .catch(e => exports.X_Foo_Fail(taskId, serialize(String(e?.message ?? e), $s.String)));
    };
}
get fooSerialized () { /* debug-mode indirection unchanged */ return this.fooSerializedHandler; }
```

`Promise.resolve().then(...)` lets us handle both sync and async user handlers uniformly without `try`/`catch` inside.

### 2.3 String marshaling for error messages

Reuse the existing serializer rather than passing raw `char*`. The `String` type is already a `SerializedMeta` entry — we encode error messages via `Serialize(e.Message)` on both sides.

### 2.4 Registries

Both must be zero-allocation on the hot path. Per call: one `int` ID, one slot lookup, no dictionary walks.

- **C#-side `PendingImports`** — int → erased `object` (the generated completion callsite knows the concrete `T` and casts via `Unsafe.As<>`). Lives in `Bootsharp.Common` so the generated code references a stable type.
  - Array-backed slot pool with a free-list stack of recycled IDs.
  - WASM is single-threaded; no locks, no `Interlocked`.
  - API:
    ```cs
    public static int Allocate (object tcs);              // returns a free ID
    public static TaskCompletionSource<T> Take<T> (int id); // pops the slot, returns the TCS
    ```
  - Internal storage: `object?[] slots`, `int[] freeList`, `int freeCount`, `int next`.
- **JS-side `pendingExports`** — same shape: a sparse array indexed by integer ID, free-list of recycled IDs. Each slot holds `{ resolve, reject, sm }` where `sm` is the result `SerializedMeta` entry (or null for void return).

Place the C# registry in `src/cs/Bootsharp.Common/Interop/PendingImports.cs`. Place the JS registry in a new `src/js/src/tasks.mts` and re-export from the runtime barrel so generated modules can `import { pendingExports } from "../tasks.mjs"`.

### 2.5 Exception propagation

- **Debug mode** (current behavior preserved):
  - C# → JS failures: serialize `$"{e.Message}\n{e.StackTrace}"`. JS-side `pendingExports.fail` constructs `new Error(message)`.
  - JS → C# failures: serialize `$"{e.message}\n{e.stack}"` (use `JSON.stringify`-style fallback for non-`Error` throwables). C# raises `JSException`.
- **Release mode**:
  - C# → JS: `e.Message` only.
  - JS → C#: `String(e?.message ?? e)` only.

`JSException` is still the C# exception type users catch — we keep that contract.

### 2.6 Where the generated callbacks live

For symmetry with the existing surface naming, generate the notify/fail/complete partials in the same `Bootsharp.Generated.Interop` static class. The JS-side notify/fail helpers attach to the `Bootsharp` module that `runtime.setModuleImports("Bootsharp", { … })` registers — the same place the `instances` helpers already live. The JS-side complete/fail helpers (called from C# via JSExport) are reachable through the same `exports` indirection the rest of the generator already uses.

### 2.7 Reentrancy and ordering

Async completions are called from a JS `Promise.then` microtask. C#-side completion is sync from the JS callback's perspective. Single-threaded WASM — no special ordering needed.

## 3. Generator changes

### 3.1 [InteropGenerator.cs](src/cs/Bootsharp.Publish/GenerateCS/InteropGenerator.cs)

Re-read the file before editing — context: the existing `ShouldWait` gate (line 225) only triggers the async path for serialized/instanced returns. **After this migration, every `method.Async == true` method uses the bridge**, regardless of return type. Primitive-return async methods are currently sync-wrapped via .NET's runtime; they'll now use the bridge too. This is consistent and keeps the generator simple.

Required changes:

- Drop the `MarshalAmbiguous` `JSType.Promise<…>` branch. Sync `Task` / `Task<T>` returns no longer exist in generated code.
- For `MethodMeta { Async: true, IK: Export }`:
  - Replace the `async Task<long>` JSExport with a `void` JSExport that takes `(int taskId, …args)` and dispatches via a fire-and-forget inner `async Task` local function.
  - Emit two sibling `[JSImport]` partials: `{Id}_{Name}_Notify (int taskId[, long resultHandle])` and `{Id}_{Name}_Fail (int taskId, long messageHandle)`.
  - The notify endpoint is `"{JSNode}.{jsName}Notify"`, fail is `"{JSNode}.{jsName}Fail"`.
- For `MethodMeta { Async: true, IK: Import }`:
  - Replace the `Task<long>` JSImport with a `void` JSImport that takes `(int taskId, …args)`.
  - Emit two JSExport partials: `{Id}_{Name}_Complete (int taskId[, long resultHandle])` and `{Id}_{Name}_Fail (int taskId, long messageHandle)`.
  - The public C# wrapper becomes: allocate a `TaskCompletionSource<T>`, call the void JSImport with the ID, return `tcs.Task`.
- Helpers in `Bootsharp.Common.Interop.PendingImports` (see §2.4) called by the generated `_Complete` / `_Fail` exports.

Methods that are `Async == true` but currently sync-wrapped (primitive return) — handle them through the bridge too. The bridge cost is uniform; treating them differently doubles the generator's emit code with no upside.

Properties and events stay synchronous — no async bridge.

### 3.2 [JSModuleGenerator.cs](src/cs/Bootsharp.Publish/GenerateJS/JSModuleGenerator.cs)

- For async exports, replace `async (...) => await exports.{fn}(...)` with `(...) => new Promise((resolve, reject) => { const id = $t.allocate(resolve, reject, $s.Result); exports.{fn}(id, ...); })`. `$t` is the new `pendingExports` registry import.
- Emit two sibling members on the Bootsharp module surface for each async export — these are the notify/fail endpoints the C# JSImport resolves against. The `JSImportsGenerator` already aggregates the Bootsharp module; the per-module `JSModuleGenerator` is responsible for emitting these named functions adjacent to the user-facing wrapper.
- For async imports, replace the `async (...) => serialize(await this.fooHandler(...), $s.Result)` serialized-handler shape with the `(taskId, argsHandle) => { Promise.resolve().then(...).then(complete).catch(fail); }` shape, where `complete` / `fail` are `exports.{Id}_{Name}_Complete` / `_Fail`.
- The `debug ? getImport(...) : srd` indirection stays — that's how we detect unassigned handlers today.
- Update the `ShouldWait` check to mean "is this method async at all" rather than "is it async and serialized/instanced". Same simplification as on the C# side.

### 3.3 [JSImportsGenerator.cs](src/cs/Bootsharp.Publish/GenerateJS/JSImportsGenerator.cs)

Minimal changes — still emits the `runtime.setModuleImports("...", {…})` bindings. Make sure the Bootsharp module aggregate still picks up the new notify/fail helpers emitted by `JSModuleGenerator`.

### 3.4 [InstanceGenerator.cs](src/cs/Bootsharp.Publish/GenerateCS/InstanceGenerator.cs)

No changes expected. Instance proxy emission delegates async methods to `Interop.{Name}` which is rewritten in §3.1 — the proxy itself stays the same.

### 3.5 [SerializerGenerator.cs](src/cs/Bootsharp.Publish/GenerateCS/SerializerGenerator.cs) / [JSSerializerGenerator.cs](src/cs/Bootsharp.Publish/GenerateJS/JSSerializerGenerator.cs)

No changes. The error-message channel reuses the existing `String` serializer entry on both sides.

### 3.6 [GenerateCS.cs](src/cs/Bootsharp.Publish/GenerateCS/GenerateCS.cs) / [GenerateJS.cs](src/cs/Bootsharp.Publish/GenerateJS/GenerateJS.cs)

- `GenerateJS.LLVM` is removed (the flag no longer exists in MSBuild). The inspection branch that filtered by `*.wasm` for Mono inspection is also dropped — always read from the full set of `*.dll` in the inspected dir, like LLVM does today.
- `GenerateJS.Debug` stays.

## 4. JS runtime layer changes

### 4.1 New: [tasks.mts](src/js/src/tasks.mts)

`pendingExports` array-backed registry per §2.4. Exported helpers `allocate(resolve, reject, sm)`, `complete(taskId, resultHandle)`, `fail(taskId, messageHandle)`.

### 4.2 [imports.mts](src/js/src/imports.mts)

Re-export the `pendingExports` registry as part of the Bootsharp module's setModuleImports payload, so the C# JSImports `"Bootsharp.{fn}Notify"` / `"Bootsharp.{fn}Fail"` resolve to it. The generated `imports.g.mjs` continues to wire user modules; the static `imports.mts` gains the `pendingExports` namespace.

### 4.3 [exports.mts](src/js/src/exports.mts)

No structural change. The new `_Complete` / `_Fail` JSExports flow through the existing `exports` indirection.

### 4.4 [boot.mts](src/js/src/boot.mts)

No structural change. The lifecycle (`create → setRuntime → bindImports → runMain → bindExports`) stays.

### 4.5 [runtime.mts](src/js/src/runtime.mts)

No changes.

## 5. Build pipeline changes

### 5.1 [Bootsharp.props](src/cs/Bootsharp/Build/Bootsharp.props)

- Delete the `BsLlvm` property and all `Condition="$(BsLlvm)"` branches. LLVM is always on.
- Inline the contents of the `Condition="$(BsLlvm)"` property group (DotNetJsApi, EmccFlags, UsingBrowserRuntimeWorkload) into the unconditional block.
- The `EmccFlags` value depends on `BsDebug`:
  - Debug: `$(EmccFlags) -O0` (or just no `-O3` addition — let Emscripten's default kick in).
  - Release: `$(EmccFlags) -O3` as today.
- Drop the Mono-only props (`WasmGenerateAppBundle`, `WasmEnableLegacyJsInterop`, `WasmEnableSIMD`) unless they remain relevant for LLVM — verify against the bench sample's csproj; what doesn't appear there can probably go.
- Always import `Microsoft.DotNet.ILCompiler.LLVM.props` (drop the `Condition="$(BsLlvm)"` guard).

### 5.2 [Bootsharp.targets](src/cs/Bootsharp/Build/Bootsharp.targets)

- Delete all `Condition="$(BsLlvm)"` and `Condition="!$(BsLlvm)"` branches.
- `BsPackAfter` is always `CopyNativeBinary` — drop the Mono `WasmNestedPublishApp` path.
- Always import `Microsoft.DotNet.ILCompiler.LLVM.targets` and the `PackageDefinitions` / `EmscriptenEnvVars`.
- `BsBuildDir` is always `$(PublishDir)`.

### 5.3 Sanity checks

- Verify nothing else under `src/cs/Bootsharp/Build/` references the deleted `BsLlvm` property.
- Update `Bootsharp.csproj` if it directly conditions on `BsLlvm` anywhere.

## 6. Test strategy

**Coverage is explicitly out of scope for this task.** Don't run `npm run cover` or the C# coverage script and don't gate on the 100% threshold. AGENTS.md's coverage policy still applies to the codebase in general — it just isn't a deliverable here, and chasing the threshold during the migration adds churn without informing the design.

- **C# generator snapshot tests** (`Bootsharp.Publish.Test`): update expected output. Inspect last-failed-test-dump.txt when assertions fail. The goal is "tests pass," not "every branch is exercised."
- **JS E2E tests** (`src/js/test`): the existing suite already exercises async round-trips (sync method, sync property, async `Task` method, async `Task<T>` with primitive/serialized/instance return, events, both-side failure cases). Keep it passing. Don't add new tests just for coverage — only add a test when you find a real gap.
- Per AGENTS.md packaging procedure, when running the JS E2E suite:
  1. `src/js`: `npm run build`
  2. `src/cs/Directory.Build.props`: bump `-alpha.N`
  3. `src/cs`: `bash .scripts/pack.sh`
  4. `src/js`: `npm run compile-test`
  5. `src/js`: `npm run test`

## 7. Staged rollout

Single stage — flag-gated parallel implementations aren't needed at this scope.

1. **Implement registries**: C# `PendingImports` + JS `pendingExports`.
2. **Rewrite generator async paths** (`InteropGenerator` + `JSModuleGenerator`) in one go.
3. **Delete `BsLlvm`** and Mono branches in props/targets.
4. **Update C# snapshot tests** until they pass.
5. **Run the full packaging + E2E loop** per §6.
6. **Iterate on E2E failures** until green.

## 8. Known issues (skipped tests)

Switching from Mono-in-debug to LLVM-in-debug surfaces five pre-existing assumptions that no longer hold. Tests covering them are `it.skip`'d with comments pointing back here. Resolve in follow-up tasks.

1. **Sync C# → JS exception messages are lost.** NativeAOT-LLVM's runtime wraps the exception in `new Error("C# exception from NativeAOT")` and discards `JSException.Message`. Affected tests:
   - `interop.spec.ts > errs when invoking unassigned imported function`
   - `interop.spec.ts > can catch dotnet exceptions`
   Fix path: add a `DotNetPatcher` patch to replace the `Ot(e)` (or equivalent) function in `dotnet.runtime.js` so it extracts the C# message via `Nt(e)`-style memory read.
2. **ICU `.dat` files and per-assembly `.wasm` no longer exist.** LLVM emits one `dotnet.native.wasm`; ICU loading goes through different MSBuild knobs. Affected tests:
   - `boot.spec.ts > uses full globalization mode when full ICU resource is present`
   - `boot.spec.ts > uses sharded globalization mode when sharded ICU resource is present`
   - `boot.spec.ts > fetches resources when root is specified` (also expects `Bootsharp.Common.wasm`)
   Fix path: rework `resources.mts` / `config.mts` to enumerate the LLVM artifact set; update the boot tests to expect the new layout.
3. **`Error.prepareStackTrace` overflow on WASM frames.** Vite's source-map hook recurses to a `RangeError` when given `wasm://` URLs from `.NET`'s `RhpGetCurrentBrowserThreadStackTrace`. Mitigated by deleting `Error.prepareStackTrace` in the test bootstrap (`test/cs.ts`). Not a code regression but worth documenting — if a new test relies on a stack-mapped frame, it will fail until Vite handles wasm frames.

## 9. Open questions

Decisions for the implementer to make as they go:

1. **Allocation per async call**. We pay one `TaskCompletionSource<T>` allocation per import call and one closure per export call. Acceptable for v1. Future: `IValueTaskSource`-based pooling.
2. **`PendingImports` slot pool initial size**. 64 seems fine; grows on demand. Optimize if profiling shows it matters.
3. **Failure message format in debug mode**. We currently propagate `$"{message}\n{stack}"`. Keep that exact format for compat with any user-facing diagnostics.
4. **Where the JS `pendingExports` namespace sits**. Suggested: `src/js/src/tasks.mts`, re-exported into `imports.mts` for `setModuleImports("Bootsharp", { … })` to pick up.

## 10. Files to read first (in this order)

1. [InteropGenerator.cs](src/cs/Bootsharp.Publish/GenerateCS/InteropGenerator.cs) — the main file being modified.
2. [JSModuleGenerator.cs](src/cs/Bootsharp.Publish/GenerateJS/JSModuleGenerator.cs) — sister file on JS side.
3. [JSImportsGenerator.cs](src/cs/Bootsharp.Publish/GenerateJS/JSImportsGenerator.cs) — to confirm minimal touch.
4. [Bootsharp.Common/Interop/](src/cs/Bootsharp.Common/Interop/) — where `PendingImports` will live.
5. [imports.mts](src/js/src/imports.mts) — to thread `pendingExports` into the Bootsharp setModuleImports payload.
6. [Bootsharp.props](src/cs/Bootsharp/Build/Bootsharp.props) + [Bootsharp.targets](src/cs/Bootsharp/Build/Bootsharp.targets) — `BsLlvm` deletion.
7. A handful of `Bootsharp.Publish.Test` snapshot tests for async exports/imports to understand the expected output shape.
