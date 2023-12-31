import { RuntimeConfig, AssetEntry, AssetBehaviors, getRuntime, getNative, getMain } from "./modules";
import { BinaryResource, BootResources } from "./resources";
import { decodeBase64 } from "./decoder";

export async function buildConfig(resources: BootResources, root?: string): Promise<RuntimeConfig> {
    const embed = root == null;
    const main = embed ? await getMain() : undefined;
    const native = embed ? await getNative() : undefined;
    const runtime = embed ? await getRuntime() : undefined;
    const mt = !embed && (await import("./dotnet.g")).mt;
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
        const url = `${root}/${res.name}`;
        return {
            // Due to dotnet bug resolvedUrl is not transferred to worker before the runtime
            // is initialized, hence we're assigning URL to the name for the JS and WASM modules
            // (assemblies are not affected). This is only relevant for multithreading mode.
            // TODO: Revise after dotnet fix https://github.com/dotnet/runtime/issues/93133.
            name: (!mt || res.content || behavior === "assembly") ? res.name : url,
            resolvedUrl: (res.content || !root) ? undefined : url,
            buffer: typeof res.content === "string" ? decodeBase64(res.content) : res.content,
            moduleExports: module,
            isOptional: optional,
            behavior
        };
    }
}
