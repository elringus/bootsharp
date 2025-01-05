import { RuntimeConfig, AssetEntry, AssetBehaviors, getRuntime, getNative, getMain } from "./modules";
import { BinaryResource, BootResources } from "./resources";
import { decodeBase64 } from "./decoder";

/** Builds .NET runtime configuration.
 *  @param resources Resources required for runtime initialization.
 *  @param root When specified, assumes boot resources are side-loaded from the specified root. */
export async function buildConfig(resources: BootResources, root?: string): Promise<RuntimeConfig> {
    const embed = root == null;
    const assets: AssetEntry[] = await Promise.all([
        resolveWasm(),
        resolveModule("dotnet.js", "js-module-dotnet", embed ? getMain : undefined),
        resolveModule("dotnet.native.js", "js-module-native", embed ? getNative : undefined),
        resolveModule("dotnet.runtime.js", "js-module-runtime", embed ? getRuntime : undefined),
        ...resources.assemblies.map(resolveAssembly)
    ]);
    const mt = !embed && (await import("./dotnet.g")).mt;
    if (mt) assets.push(await resolveModule("dotnet.native.worker.mjs", "js-module-threads"));
    return { assets, mainAssemblyName: resources.entryAssemblyName };

    async function resolveWasm(): Promise<AssetEntry> {
        return {
            name: resources.wasm.name,
            buffer: await resolveBuffer(resources.wasm),
            behavior: "dotnetwasm"
        };
    }

    async function resolveModule(name: string, behavior: AssetBehaviors,
        embed?: () => Promise<unknown>): Promise<AssetEntry> {
        return {
            name,
            moduleExports: embed ? await embed() : undefined,
            behavior
        };
    }

    async function resolveAssembly(res: BinaryResource): Promise<AssetEntry> {
        return {
            name: res.name,
            buffer: await resolveBuffer(res),
            behavior: "assembly"
        };
    }

    async function resolveBuffer(res: BinaryResource): Promise<ArrayBuffer> {
        if (typeof res.content === "string") return decodeBase64(res.content);
        if (res.content !== undefined) return <never>res.content.buffer;
        if (!embed) return fetchBuffer(res);
        throw Error(`Failed to resolve '${res.name}' boot resource.`);
    }

    async function fetchBuffer(res: BinaryResource): Promise<ArrayBuffer> {
        const path = `${root}/${res.name}`;
        if (typeof window === "object")
            return (await fetch(path)).arrayBuffer();
        if (typeof process === "object") {
            const { readFile } = await import("fs/promises");
            const bin = await readFile(path);
            return <ArrayBuffer>bin.buffer.slice(bin.byteOffset, bin.byteOffset + bin.byteLength);
        }
        throw Error(`Failed to fetch '${path}' boot resource: unsupported runtime.`);
    }
}
