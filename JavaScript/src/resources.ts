// @ts-ignore (resolved at C# solution build time)
import generated from "./bootsharp-resources.js";

/** Resources required to boot .NET runtime. */
export const resources: BootResources = generated;

/** Resources required to boot .NET runtime. */
export type BootResources = {
    /** Either binary or base64-encoded string of the runtime WASM content. */
    wasm?: Uint8Array | string;
    /** C# assemblies required to boot the runtime. */
    readonly assemblies: AssemblyResource[];
    /** Name of the entry (main) assembly, without file extension. */
    readonly entryAssemblyName: string;
}

/** C# assembly required to boot .NET runtime. */
export type AssemblyResource = {
    /** Name of the assembly, without file extension. */
    readonly name: string;
    /** Either binary or base64-encoded string of the assembly content. */
    content?: Uint8Array | string;
}

export function validateResources(res: BootResources) {
    if (res.wasm == null || res.wasm.length === 0)
        throw Error("Missing WASM boot resource.");
    for (const asm of res.assemblies)
        if (asm.content == null || asm.content.length === 0)
            throw Error(`Missing '${asm.name}' assembly boot resource.`);
    return res;
}
