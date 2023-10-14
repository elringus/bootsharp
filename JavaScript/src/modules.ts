import type { ModuleAPI, MonoConfig, AssetEntry } from "./dotnet.g.d.ts";

export type * from "./dotnet.g.d.ts";
export type RuntimeConfig = MonoConfig & { assets?: AssetEntry[] };

/** Fetches main dotnet module (<code>dotnet.js</code>). */
export async function getMain(root?: string): Promise<ModuleAPI> {
    if (root == null) return await import("./dotnet.g");
    return await import(/*@vite-ignore*//*webpackIgnore:true*/`${root}/dotnet.js`);
}

/** Fetches dotnet native module (<code>dotnet.native.js</code>). */
export async function getNative(root?: string): Promise<unknown> {
    if (root == null) return await import("./dotnet.native.g");
    return await import(/*@vite-ignore*//*webpackIgnore:true*/`${root}/dotnet.native.js`);
}

/** Fetches dotnet runtime module (<code>dotnet.runtime.js</code>). */
export async function getRuntime(root?: string): Promise<unknown> {
    if (root == null) return await import("./dotnet.runtime.g");
    return await import(/*@vite-ignore*//*webpackIgnore:true*/`${root}/dotnet.runtime.js`);
}
