import type { RuntimeAPI } from "./dotnet/index.mjs";

/** The Emscripten Module. C# exports are exposed via underscore-prefixed properties
 *  (eg, an export named "Foo" is accessed as <code>exports._Foo</code>). */
export let exports: Record<string, unknown>;

export function bindExports(runtime: RuntimeAPI) {
    exports = runtime.Module as unknown as Record<string, unknown>;
}

// noinspection JSUnusedGlobalSymbols (used by the generated code in debug mode)
export function getExport(name: string): (...args: unknown[]) => unknown {
    return (...args) => {
        if (exports == null) throw Error("Boot the runtime before invoking C# APIs.");
        let result: unknown;
        try { result = (exports["_" + name] as (...args: unknown[]) => unknown)(...args); }
        catch (error) { throw Error(`${(error as Error).message}\n${(error as Error).stack}`); }
        if (typeof (result as { then?: unknown })?.then === "function")
            return (result as Promise<unknown>).catch(error => {
                throw Error(`${(error as Error).message}\n${(error as Error).stack}`);
            });
        return result;
    };
}
