import generated from "./resources.g";
import { RuntimeConfig, AssetEntry, runtime, native, AssetBehaviors } from "./external";
import { decodeBase64 } from "./decoder";

/** Resources required to boot .NET runtime. */
export type BootResources = {
    /** Compiled .NET WASM runtime module. */
    readonly wasm: BinaryResource;
    /** Compiled .NET assemblies. */
    readonly assemblies: BinaryResource[];
    /** Name of the entry (main) assembly, without file extension. */
    readonly entryAssemblyName: string;
    /** URL of the remote directory where boot resources are hosted (eg, <code>/bin</code>).
     *  When resource content is missing in boot config, will attempt to fetch it from the remote.
     *  Required for streaming WASM compilation and multithreading mode. */
    root?: string;
}

/** Binary resource required to boot .NET runtime.*/
export type BinaryResource = {
    /** Name of the binary file, including extension. */
    readonly name: string;
    /** Embedded binary or base64-encoded string of the resource content. */
    content?: Uint8Array | string;
}

/** Resources required to boot .NET runtime. */
export const resources: BootResources = generated;

export function buildConfig(): RuntimeConfig {
    return {
        mainAssemblyName: resources.entryAssemblyName,
        assets: [
            buildResource({ name: "dotnet.runtime.js" }, "js-module-runtime", runtime),
            buildResource({ name: "dotnet.native.js" }, "js-module-native", native),
            buildResource({ name: "dotnet.native.worker.js" }, "js-module-threads"),
            buildResource(resources.wasm, "dotnetwasm"),
            ...resources.assemblies.map(a => buildResource(a, "assembly"))
        ]
    };
}

function buildResource(res: BinaryResource, behavior: AssetBehaviors, module?: unknown): AssetEntry {
    return {
        name: (!resources.root || res.content) ? res.name : `${resources.root}/${res.name}`,
        buffer: res.content ? toBinary(res.content) : undefined,
        moduleExports: module,
        behavior
    };
}

function toBinary(data: Uint8Array | string): Uint8Array {
    if (typeof data !== "string") return data;
    if (typeof window === "object") return Uint8Array.from(window.atob(data), c => c.charCodeAt(0));
    if (typeof Buffer === "function") return Buffer.from(data, "base64");
    return decodeBase64(data);
}
