import type { RuntimeAPI } from "./dotnet/index.mjs";

export let exports: Record<string, unknown>;

export async function bindExports(runtime: RuntimeAPI, assembly: string) {
    const asm = await runtime.getAssemblyExports(assembly);
    exports = asm["Bootsharp"]["Generated"]["Interop"] ?? {};
    exports.disposeExported = asm["Bootsharp"]["Generated"]["Instances"].DisposeExported;
}
