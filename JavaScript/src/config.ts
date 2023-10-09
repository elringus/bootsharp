import { RuntimeConfig, AssetEntry, AssetBehaviors, getRuntime, getNative, getMain } from "./modules";
import { resources, BinaryResource } from "./resources";
import { decodeBase64 } from "./decoder";

export async function buildConfig(): Promise<RuntimeConfig> {
    const embed = resources.root == null;
    const main = embed ? await getMain() : undefined;
    const native = embed ? await getNative() : undefined;
    const runtime = embed ? await getRuntime() : undefined;
    return {
        mainAssemblyName: resources.entryAssemblyName,
        assets: [
            buildAsset({ name: "dotnet.js" }, "js-module-dotnet", main),
            buildAsset({ name: "dotnet.native.js" }, "js-module-native", native),
            buildAsset({ name: "dotnet.runtime.js" }, "js-module-runtime", runtime),
            buildAsset({ name: "dotnet.native.worker.js" }, "js-module-threads", undefined, true),
            buildAsset(resources.wasm, "dotnetwasm"),
            ...resources.assemblies.map(a => buildAsset(a, "assembly"))
        ]
    };
}

function buildAsset(res: BinaryResource, behavior: AssetBehaviors,
    module?: unknown, optional?: boolean): AssetEntry {
    const url = `${resources.root}/${res.name}`;
    return {
        // TODO: Revise after dotnet fix (https://github.com/dotnet/runtime/issues/93133)
        name: (res.content || behavior === "assembly") ? res.name : url,
        resolvedUrl: (res.content || !resources.root) ? undefined : url,
        buffer: typeof res.content === "string" ? decodeBase64(res.content) : res.content,
        moduleExports: module,
        isOptional: optional,
        behavior
    };
}
