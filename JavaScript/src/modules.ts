import type { ModuleAPI, MonoConfig, AssetEntry } from "./dotnet.g.d.ts";
import { resources } from "./resources";

export type * from "./dotnet.g.d.ts";
export type RuntimeConfig = MonoConfig & { assets?: AssetEntry[] };

/** Fetches main dotnet module (<code>dotnet.js</code>). */
export async function getMain(): Promise<ModuleAPI> {
    if (resources.root) return await import(`${resources.root}/dotnet.js`);
    return await import("./dotnet.g");
}

/** Fetches dotnet runtime module (<code>dotnet.runtime.js</code>). */
export async function getRuntime(): Promise<unknown> {
    if (resources.root) return await import(`${resources.root}/dotnet.runtime.js`);
    return await import("./dotnet.runtime.g");
}

/** Fetches dotnet native module (<code>dotnet.native.js</code>). */
export async function getNative(): Promise<unknown> {
    if (resources.root) return await import(`${resources.root}/dotnet.native.js`);
    return await import("./dotnet.native.g");
}
