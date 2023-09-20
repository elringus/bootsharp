import type { RuntimeAPI } from "./dotnet.d.ts";

export let exports: unknown;

export async function bindExports(runtime: RuntimeAPI, assembly: string) {
    const asm = await runtime.getAssemblyExports(assembly);
    exports = asm["Bootsharp"]?.["Exports"];
}
