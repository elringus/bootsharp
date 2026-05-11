import type { RuntimeAPI } from "./dotnet/index.mjs";

export let exports: Record<string, unknown>;

export async function bindExports(runtime: RuntimeAPI, assembly: string) {
    const asm = await runtime.getAssemblyExports(assembly);
    exports = asm["Bootsharp"]["Generated"]["Interop"] ?? {};
    exports.disposeExported = asm["Bootsharp"]["Generated"]["Instances"].DisposeExported;
}

// noinspection JSUnusedGlobalSymbols (used by the generated code in debug mode)
export function getExport(name: string): (...args: unknown[]) => unknown {
    return (...args) => {
        if (exports == null) throw Error("Boot the runtime before invoking C# APIs.");
        let result: unknown;
        try { result = (exports[name] as (...args: unknown[]) => unknown)(...args); }
        catch (error) { throw Error(`${(error as Error).message}\n${(error as Error).stack}`); }
        if (typeof (result as { then?: unknown })?.then === "function")
            return (result as Promise<unknown>).catch(error => {
                throw Error(`${(error as Error).message}\n${(error as Error).stack}`);
            });
        return result;
    };
}
