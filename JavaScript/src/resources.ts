// @ts-expect-error (resolved when building C# solution)
import generated from "./resources.g";
import { RuntimeConfig, AssetEntry, runtime, native } from "./external";

/** Resources required to boot .NET runtime. */
export const resources: BootResources = generated;

/** Resources required to boot .NET runtime. */
export type BootResources = {
    /** Either binary or base64-encoded string of the runtime WASM content. */
    wasm: BinaryResource;
    /** C# assemblies required to boot the runtime. */
    readonly assemblies: BinaryResource[];
    /** Name of the entry (main) assembly, without file extension. */
    readonly entryAssemblyName: string;
}

/** Binary resource required to boot .NET runtime.*/
export type BinaryResource = {
    /** Name of the binary file, including extension. */
    readonly name: string;
    /** Either binary or base64-encoded string of the file content. */
    content?: Uint8Array | string;
}

// https://github.com/dotnet/runtime/tree/main/src/mono/sample/wasm/browser-minimal-config
export function buildConfig(): RuntimeConfig {
    validate(resources);
    return {
        mainAssemblyName: resources.entryAssemblyName,
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
                buffer: toBinary(resources.wasm.content!),
                behavior: "dotnetwasm"
            },
            ...resources.assemblies.map(buildAssembly)
        ]
    };
}

function validate(res: BootResources): void {
    if (res.wasm.content == null || res.wasm.content.length === 0)
        throw Error("Missing WASM boot resource.");
    for (const asm of res.assemblies)
        if (asm.content == null || asm.content.length === 0)
            throw Error(`Missing '${asm.name}' assembly boot resource.`);
}

function buildAssembly(res: BinaryResource): AssetEntry {
    return {
        name: res.name,
        buffer: toBinary(res.content!),
        behavior: "assembly"
    };
}

function toBinary(data: Uint8Array | string): Uint8Array {
    if (typeof data !== "string") return data;
    if (typeof window === "object") return Uint8Array.from(window.atob(data), c => c.charCodeAt(0));
    if (typeof Buffer === "function") return Buffer.from(data, "base64");

    const abc = [..."ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"];
    const result = [];
    for (let i = 0; i < data.length / 4; i++) {
        const chunk = [...data.slice(4 * i, 4 * i + 4)];
        const bin = chunk.map(x => abc.indexOf(x).toString(2).padStart(6, "0")).join("");
        const bytes = bin.match(/.{1,8}/g)?.map(x => +("0b" + x)) ?? [];
        result.push(...bytes.slice(0, 3 - Number(data[4 * i + 2] == "=") - Number(data[4 * i + 3] == "=")));
    }
    return Uint8Array.from(result);
}
