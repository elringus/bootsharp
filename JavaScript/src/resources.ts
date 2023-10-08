import generated from "./resources.g";
import { RuntimeConfig, AssetEntry, AssetBehaviors, runtime, native } from "./external";
import { decodeBase64 } from "./decoder";

/** Resources required to boot .NET runtime. */
export type BootResources = {
    /** Compiled .NET WASM runtime module. */
    readonly wasm: BinaryResource;
    /** Compiled .NET assemblies. */
    readonly assemblies: BinaryResource[];
    /** Name of the entry (main) assembly, with .dll extension. */
    readonly entryAssemblyName: string;
    /** URL of the remote directory where boot resources are hosted (eg, <code>/bin</code>).
     *  Has to be assigned before boot when <code>BootsharpEmbedBinaries</code> is disabled. */
    root?: string;
}

/** Boot resource with binary content. */
export type BinaryResource = {
    /** Name of the binary file, including extension. */
    readonly name: string;
    /** Binary or base64-encoded content of the file; undefined when embedding disabled. */
    content?: Uint8Array | string;
}

/** Resources required to boot .NET runtime. */
export const resources: BootResources = generated;

export function buildConfig(): RuntimeConfig {
    return {
        mainAssemblyName: resources.entryAssemblyName,
        assets: [
            buildAsset({ name: "dotnet.runtime.js" }, "js-module-runtime", runtime),
            buildAsset({ name: "dotnet.native.js" }, "js-module-native", native),
            buildAsset({ name: "dotnet.native.worker.js" }, "js-module-threads", {}, true),
            buildAsset(resources.wasm, "dotnetwasm"),
            ...resources.assemblies.map(a => buildAsset(a, "assembly"))
        ]
    };
}

function buildAsset(res: BinaryResource, behavior: AssetBehaviors,
    module?: { embedded?: boolean }, optional?: boolean): AssetEntry {
    const url = `${resources.root}/${res.name}`;
    return {
        name: (res.content || behavior === "assembly") ? res.name : url,
        resolvedUrl: (res.content || !resources.root) ? undefined : url,
        buffer: typeof res.content === "string" ? decodeBase64(res.content) : res.content,
        moduleExports: module?.embedded === false ? undefined : module,
        isOptional: optional,
        behavior
    };
}
