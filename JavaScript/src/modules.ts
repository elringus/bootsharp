import type { ModuleAPI, MonoConfig, AssetEntry } from "./dotnet.g.d.ts";
import { resources } from "./resources";

export type * from "./dotnet.g.d.ts";
export type RuntimeConfig = MonoConfig & { assets?: AssetEntry[] };

/** Fetches main dotnet module (<code>dotnet.js</code>). */
export async function getMain(): Promise<ModuleAPI> {
    if (resources.root == null) return await import("./dotnet.g");
    return await import(/*@vite-ignore*//*webpackIgnore:true*/`${resources.root}/dotnet.js`);
}

/** Fetches dotnet native module (<code>dotnet.native.js</code>). */
export async function getNative(): Promise<unknown> {
    if (resources.root == null) return await import("./dotnet.native.g");
    return await import(/*@vite-ignore*//*webpackIgnore:true*/`${resources.root}/dotnet.native.js`);
}

/** Fetches dotnet runtime module (<code>dotnet.runtime.js</code>). */
export async function getRuntime(): Promise<unknown> {
    if (resources.root == null) return await import("./dotnet.runtime.g");
    return await import(/*@vite-ignore*//*webpackIgnore:true*/`${resources.root}/dotnet.runtime.js`);
}
