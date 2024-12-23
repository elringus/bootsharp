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
    // const mt = !embed && (await import("./dotnet.g")).mt;
    return {
        mainAssemblyName: resources.entryAssemblyName,
        assets: [
            buildAsset({ name: "dotnet.js" }, "js-module-dotnet", main, false),
            buildAsset({ name: "dotnet.native.js" }, "js-module-native", native, false),
            buildAsset({ name: "dotnet.runtime.js" }, "js-module-runtime", runtime, false),
            buildAsset({ name: "dotnet.native.worker.js" }, "js-module-threads", undefined, true),
            buildAsset(resources.wasm, "dotnetwasm", undefined, false),
            ...resources.assemblies.map(a => buildAsset(a, "assembly"))
        ]
    };

    function buildAsset(res: BinaryResource, behavior: AssetBehaviors,
        module?: unknown, optional?: boolean): AssetEntry {
        return {
            name: res.name,
            resolvedUrl: (res.content || !root) ? undefined : `${root}/${res.name}`,
            buffer: typeof res.content === "string" ? decodeBase64(res.content) : <never>res.content?.buffer,
            moduleExports: module,
            isOptional: optional,
            behavior
        };
    }
}
