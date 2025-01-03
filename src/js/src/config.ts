import { RuntimeConfig, AssetEntry, AssetBehaviors, getRuntime, getNative, getMain } from "./modules";
import { BinaryResource, BootResources } from "./resources";
import { decodeBase64 } from "./decoder";

/** Builds .NET runtime configuration.
 *  @param resources Resources required for runtime initialization.
 *  @param root When specified, assumes boot resources are side-loaded from the specified root. */
export async function buildConfig(resources: BootResources, root?: string): Promise<RuntimeConfig> {
    const embed = root == null;
    const main = embed ? await getMain() : undefined;
    const native = embed ? await getNative() : undefined;
    const runtime = embed ? await getRuntime() : undefined;
    const mt = !embed && (await import("./dotnet.g")).mt;
    const assets: AssetEntry[] = [
        buildAsset({ name: "dotnet.js" }, "js-module-dotnet", main),
        buildAsset({ name: "dotnet.native.js" }, "js-module-native", native),
        buildAsset({ name: "dotnet.runtime.js" }, "js-module-runtime", runtime),
        buildAsset(resources.wasm, "dotnetwasm", undefined),
        ...resources.assemblies.map(a => buildAsset(a, "assembly"))
    ];
    if (mt) assets.push(buildAsset({ name: "dotnet.native.worker.js" }, "js-module-threads", undefined));
    return { mainAssemblyName: resources.entryAssemblyName, assets };

    function buildAsset(res: BinaryResource, behavior: AssetBehaviors, module?: unknown): AssetEntry {
        return {
            name: res.name,
            buffer: module ? undefined : resolveBuffer(res),
            moduleExports: module,
            behavior
        };
    }

    async function resolveBuffer(res: BinaryResource): Promise<ArrayBuffer> {
        if (typeof res.content === "string") return decodeBase64(res.content);
        if (res.content !== undefined) return <never>res.content.buffer;
        const fullPath = `${root}/${res.name}`;
        if (typeof window !== "undefined") {
            return (await fetch(fullPath)).arrayBuffer();
        } else {
            const { readFile } = await import("fs/promises");
            const bin = await readFile(fullPath);
            return <never>bin.buffer.slice(bin.byteOffset, bin.byteOffset + bin.byteLength);
        }
    }
}
