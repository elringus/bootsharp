import type { ModuleAPI, MonoConfig } from "./dotnet.g.d.ts";
export type { Asset } from "./dotnet.g.d.ts";

export type * from "./dotnet.g.d.ts";
export type RuntimeConfig = MonoConfig;
export type RuntimeResources = NonNullable<RuntimeConfig["resources"]>;
export type WasmAsset = NonNullable<RuntimeResources["wasmNative"]>[number];
export type ModuleAsset = NonNullable<RuntimeResources["jsModuleNative"]>[number];
export type AssemblyAsset = NonNullable<RuntimeResources["assembly"]>[number];
export type IcuAsset = NonNullable<RuntimeResources["icu"]>[number];
export type PdbAsset = NonNullable<RuntimeResources["pdb"]>[number];
export type SymbolsAsset = NonNullable<RuntimeResources["wasmSymbols"]>[number];

/** Fetches the main dotnet module (<code>dotnet.js</code>). */
export async function getMain(root?: string): Promise<ModuleAPI & { embedded?: boolean }> {
    if (root == null) return await import("./dotnet.g");
    return await import(/*@vite-ignore*//*webpackIgnore:true*/`${root}/dotnet.js`);
}

/** Fetches dotnet native module (<code>dotnet.native.js</code>). */
export async function getNative(root?: string): Promise<unknown & { embedded?: boolean }> {
    if (root == null) return await import("./dotnet.native.g");
    return await import(/*@vite-ignore*//*webpackIgnore:true*/`${root}/dotnet.native.js`);
}

/** Fetches dotnet runtime module (<code>dotnet.runtime.js</code>). */
export async function getRuntime(root?: string): Promise<unknown & { embedded?: boolean }> {
    if (root == null) return await import("./dotnet.runtime.g");
    return await import(/*@vite-ignore*//*webpackIgnore:true*/`${root}/dotnet.runtime.js`);
}
