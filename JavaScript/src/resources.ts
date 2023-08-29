// @ts-ignore (resolved when building C# solution)
import generated from "./bootsharp-resources.js";
import { RuntimeConfig, runtime, native } from "./dotnet-api";
import { AssetEntry } from "./dotnet-types";

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

// https://github.com/dotnet/runtime/tree/main/src/mono/sample/wasm/browser-minimal-config
export function buildConfig(res?: BootResources): RuntimeConfig {
    validate(res ??= resources);
    return {
        mainAssemblyName: res.entryAssemblyName,
        assets: [
            {
                name: "dotnet.runtime.js",
                moduleExports: runtime,
                behavior: "js-module-runtime"
            },
            {
                name: "dotnet.native.js",
                moduleExports: native,
                behavior: "js-module-native"
            },
            {
                name: "dotnet.native.wasm",
                buffer: toBinary(res.wasm!),
                behavior: "dotnetwasm"
            },
            ...res.assemblies.map(buildAssembly)
        ]
    };
}

function validate(res: BootResources): void {
    if (res.wasm == null || res.wasm.length === 0)
        throw Error("Missing WASM boot resource.");
    for (const asm of res.assemblies)
        if (asm.content == null || asm.content.length === 0)
            throw Error(`Missing '${asm.name}' assembly boot resource.`);
}

function buildAssembly(res: AssemblyResource): AssetEntry {
    return {
        name: res.name,
        buffer: toBinary(res.content!),
        behavior: "assembly"
    };
}

function toBinary(data: Uint8Array | string): Uint8Array {
    if (typeof data !== "string") return data;
    return typeof Buffer === "function"
        ? new Uint8Array(Buffer.from(data, "base64"))
        : Uint8Array.from(atob(data), c => c.charCodeAt(0));
}
