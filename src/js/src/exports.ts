import type { RuntimeAPI } from "./modules";

export let exports: unknown;

export async function bindExports(runtime: RuntimeAPI, assembly: string) {
    const asm = await runtime.getAssemblyExports(assembly);
    exports = asm["Bootsharp"]?.["Generated"]["Interop"];
}
