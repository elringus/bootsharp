import generated from "./generated/resources.g.mjs";

/** Resources required to boot .NET runtime. */
export type BootResources = {
    /** Compiled .NET WASM runtime module. */
    readonly wasm: BinaryResource;
    /** Compiled .NET assemblies. */
    readonly assemblies: BinaryResource[];
    /** Globalization data. */
    readonly icu: BinaryResource[];
    /** WASM debug symbols. */
    readonly symbols: BinaryResource[];
    /** PDB debug artifacts. */
    readonly pdb: BinaryResource[];
    /** Name of the entry (main) assembly, with .dll extension. */
    readonly entryAssemblyName: string;
}

/** Boot resource with binary content. */
export type BinaryResource = {
    /** Name of the binary file, including extension. */
    readonly name: string;
    /** Binary content of the file. */
    readonly content: ArrayBuffer;
}

/** Resources required to boot .NET runtime. */
export const resources: BootResources = generated;

/** Fetches required boot resources from the specified root URL. */
export async function fetchResources(root: string): Promise<BootResources> {
    const [wasm, assemblies, icu, symbols, pdb] = await Promise.all([
        fetchResource(resources.wasm),
        Promise.all(resources.assemblies.map(fetchResource)),
        Promise.all(resources.icu.map(fetchResource)),
        Promise.all(resources.symbols.map(fetchResource)),
        Promise.all(resources.pdb.map(fetchResource))
    ]);
    return { wasm, assemblies, icu, symbols, pdb, entryAssemblyName: resources.entryAssemblyName };

    async function fetchResource(r: BinaryResource): Promise<BinaryResource> {
        const content = await (await fetch(`${root}/${r.name}`)).arrayBuffer();
        return { name: r.name, content };
    }
}
