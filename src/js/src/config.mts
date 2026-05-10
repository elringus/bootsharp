import { RuntimeConfig, Asset, WasmAsset, AssemblyAsset, IcuAsset, PdbAsset, SymbolsAsset, ModuleAsset } from "./dotnet/index.mjs";
import { BinaryResource, BootResources, manifest } from "./resources.mjs";
import * as nativeModule from "./dotnet/dotnet.native.js";
import * as runtimeModule from "./dotnet/dotnet.runtime.js";

/** Builds .NET runtime configuration from the specified boot resources. */
export function buildConfig(resources: BootResources): RuntimeConfig {
    return {
        resources: {
            wasmNative: [resolveAsset<WasmAsset>({ name: manifest.wasm, content: resources.wasm })],
            jsModuleNative: [resolveModule("dotnet.native.js", nativeModule)],
            jsModuleRuntime: [resolveModule("dotnet.runtime.js", runtimeModule)],
            assembly: resources.assemblies?.map(resolveAsset<AssemblyAsset>),
            icu: resources.icu?.map(resolveAsset<IcuAsset>),
            wasmSymbols: resources.symbols?.map(resolveSymbols),
            pdb: resources.pdb?.map(resolveAsset<PdbAsset>)
        },
        mainAssemblyName: manifest.entryAssemblyName,
        globalizationMode: resolveGlobalizationMode(),
        debugLevel: resources.symbols ? -1 : undefined
    };

    function resolveModule(name: string, exports: unknown): ModuleAsset {
        return { name, moduleExports: exports };
    }

    function resolveAsset<T extends Asset>(res: BinaryResource): T {
        return { name: res.name, virtualPath: res.name, buffer: res.content } as Asset as T;
    }

    function resolveSymbols(res: BinaryResource): SymbolsAsset {
        // Use 'resolveAsset<SymbolsAsset>()' once https://github.com/dotnet/runtime/pull/127087 is merged.
        const txt = new TextDecoder("utf-8").decode(res.content);
        return {
            name: res.name,
            pendingDownload: {
                name: res.name,
                url: res.name,
                response: Promise.resolve(new Response(txt, { status: 200 }))
            }
        };
    }

    function resolveGlobalizationMode(): RuntimeConfig["globalizationMode"] {
        if (!resources.icu) return "invariant" as never;
        if (resources.icu.some(res => res.name === "icudt.dat")) return "all" as never;
        return "sharded" as never;
    }
}
