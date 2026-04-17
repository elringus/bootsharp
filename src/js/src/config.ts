import { RuntimeConfig, RuntimeWasm, RuntimeModule, RuntimeAssembly, getRuntime, getNative } from "./modules";
import { BinaryResource, BootResources } from "./resources";
import { decodeBase64 } from "./decoder";

/** Builds .NET runtime configuration.
 *  @param resources Resources required for runtime initialization.
 *  @param root When specified, assumes boot resources are side-loaded from the specified root. */
export async function buildConfig(resources: BootResources, root?: string): Promise<RuntimeConfig> {
    const embed = root == null;
    const mt = !embed && (await import("./dotnet.g")).mt;
    const [wasm, native, runtime, assemblies] = await Promise.all([
        resolveWasm(),
        resolveModule("dotnet.native.js", embed ? getNative : undefined),
        resolveModule("dotnet.runtime.js", embed ? getRuntime : undefined),
        Promise.all(resources.assemblies.map(resolveAssembly))
    ]);
    return {
        resources: {
            wasmNative: [wasm],
            jsModuleNative: [native],
            jsModuleRuntime: [runtime],
            jsModuleWorker: mt ? [await resolveModule("dotnet.native.worker.mjs")] : undefined,
            assembly: assemblies
        },
        mainAssemblyName: resources.entryAssemblyName
    };

    async function resolveWasm(): Promise<RuntimeWasm> {
        return {
            name: resources.wasm.name,
            buffer: await resolveBuffer(resources.wasm)
        };
    }

    async function resolveModule(name: string, embed?: () => Promise<unknown>): Promise<RuntimeModule> {
        return {
            name,
            moduleExports: embed ? await embed() : undefined
        };
    }

    async function resolveAssembly(res: BinaryResource): Promise<RuntimeAssembly> {
        return {
            name: res.name,
            virtualPath: res.name,
            buffer: await resolveBuffer(res)
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
            const { readFile } = await import(/*@vite-ignore*//*webpackIgnore:true*/"fs/promises");
            const bin = await readFile(path);
            return <ArrayBuffer>bin.buffer.slice(bin.byteOffset, bin.byteOffset + bin.byteLength);
        }
        throw Error(`Failed to fetch '${path}' boot resource: unsupported runtime.`);
    }
}
