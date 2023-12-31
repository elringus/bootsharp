import generated from "./resources.g";

/** Resources required to boot .NET runtime. */
export type BootResources = {
    /** Compiled .NET WASM runtime module. */
    readonly wasm: BinaryResource;
    /** Compiled .NET assemblies. */
    readonly assemblies: BinaryResource[];
    /** Name of the entry (main) assembly, with .dll extension. */
    readonly entryAssemblyName: string;
}

/** Boot resource with binary content. */
export type BinaryResource = {
    /** Name of the binary file, including extension. */
    readonly name: string;
    /** Binary or base64-encoded content of the file; undefined when embedding disabled. */
    readonly content?: Uint8Array | string;
}

/** Resources required to boot .NET runtime. */
export const resources: BootResources = generated;
