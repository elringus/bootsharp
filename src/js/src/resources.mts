import generated from "./generated/resources.g.mjs";

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
    /** Binary content of the compiled WASM runtime module. */
    wasm: ArrayBuffer;
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
    /** Binary content of the file. */
    content: ArrayBuffer;
}>;

/** Lists resource names required to boot the runtime. */
export const manifest: BootManifest = generated;

/** Fetches required boot resources from the specified root URL. */
export async function fetchResources(root: string): Promise<BootResources> {
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
