import * as generated from "./generated/resources.g.mjs";

/** Lists resource file names (including extension) required to boot the runtime. */
export type BootManifest = Readonly<{
    /** Compiled WASM runtime module. */
    wasm: string;
    /** Compiled runtime assemblies. */
    assemblies: string[];
    /** Globalization data. */
    icu: string[];
    /** WASM debug symbols. */
    symbols: string[];
    /** PDB debug artifacts. */
    pdb: string[];
    /** Name of the entry (main) assembly. */
    entryAssemblyName: string;
}>;

/** Resources required to boot the runtime. */
export type BootResources = Readonly<{
    /** Compiled WASM content: either raw bytes or base64 encoded string. */
    wasm: ArrayBuffer | string;
    /** Compiled runtime assemblies. */
    assemblies?: BinaryResource[];
    /** Globalization data. */
    icu?: BinaryResource[];
    /** WASM debug symbols. */
    symbols?: BinaryResource[];
    /** PDB debug artifacts. */
    pdb?: BinaryResource[];
}>;

/** Boot resource with binary content. */
export type BinaryResource = Readonly<{
    /** Name of the file, including extension. */
    name: string;
    /** Binary content of the file: either raw bytes or base64 encoded string. */
    content: ArrayBuffer | string;
}>;

/** Lists resource names required to boot the runtime. */
export const manifest: BootManifest = generated.manifest;

/** Fetches resources from the specified root URL or from the embedded resources when in embedded mode. */
export async function fetchResources(root?: string): Promise<BootResources> {
    /* v8 ignore next -- embedded mode is covered in samples */
    if (generated.embedded != null) return generated.embedded;
    const [wasm, assemblies, icu, symbols, pdb] = await Promise.all([
        fetchResource(manifest.wasm),
        Promise.all(manifest.assemblies.map(fetchResource)),
        Promise.all(manifest.icu.map(fetchResource)),
        Promise.all(manifest.symbols.map(fetchResource)),
        Promise.all(manifest.pdb.map(fetchResource))
    ]);
    return { wasm: wasm.content, assemblies, icu, symbols, pdb };

    async function fetchResource(name: string): Promise<BinaryResource> {
        const content = await (await fetch(`${root}/${name}`)).arrayBuffer();
        return { name, content };
    }
}
