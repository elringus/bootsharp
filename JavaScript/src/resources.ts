import generated from "./resources.g";
import { RuntimeConfig, AssetEntry, AssetBehaviors, ResourceGroups, ResourceList, runtime, native } from "./external";
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
    const embed = resources.wasm.content != null;
    if (!embed && resources.root == null)
        throw Error("Resources root has to be specified when binaries are not embedded.");
    return {
        mainAssemblyName: resources.entryAssemblyName,
        resources: embed ? undefined : buildResources(),
        assets: embed ? buildAssets() : undefined
    };
}

function buildResources(): ResourceGroups {
    return {
        jsModuleRuntime: buildResourceList("dotnet.runtime.js"),
        jsModuleNative: buildResourceList("dotnet.native.js"),
        jsModuleWorker: buildResourceList("dotnet.native.worker.js"),
        wasmNative: buildResourceList("dotnet.native.wasm"),
        wasmSymbols: buildResourceList("dotnet.native.js.symbols"),
        assembly: buildResourceList(...resources.assemblies.map(a => a.name))
    };
}

function buildResourceList(...names: string[]): ResourceList {
    const list: ResourceList = {};
    for (const name of names)
        list[`${resources.root}/${name}`] = "";
    return list;
}

function buildAssets(): AssetEntry[] {
    const runtimeModule = runtime.default ? runtime : undefined;
    const nativeModule = native.default ? native : undefined;
    return [
        buildAsset({ name: "dotnet.runtime.js" }, "js-module-runtime", runtimeModule),
        buildAsset({ name: "dotnet.native.js" }, "js-module-native", nativeModule),
        buildAsset(resources.wasm, "dotnetwasm"),
        ...resources.assemblies.map(a => buildAsset(a, "assembly"))
    ];
}

function buildAsset(res: BinaryResource, behavior: AssetBehaviors, module?: unknown): AssetEntry {
    return {
        name: (!resources.root || res.content) ? res.name : `${resources.root}/${res.name}`,
        buffer: typeof res.content === "string" ? decodeBase64(res.content) : res.content,
        moduleExports: module,
        behavior
    };
}
