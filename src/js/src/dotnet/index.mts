// Modules under the 'src/dotnet' folder are copied when building the C# solution.

import type { ModuleAPI, MonoConfig } from "./dotnet.g.d.ts";
import * as dotnet from "./dotnet.js";

export type * from "./dotnet.g.d.ts";
export type RuntimeConfig = MonoConfig;
export type RuntimeResources = NonNullable<RuntimeConfig["resources"]>;
export type WasmAsset = NonNullable<RuntimeResources["wasmNative"]>[number];
export type ModuleAsset = NonNullable<RuntimeResources["jsModuleNative"]>[number];
export type AssemblyAsset = NonNullable<RuntimeResources["assembly"]>[number];
export type IcuAsset = NonNullable<RuntimeResources["icu"]>[number];
export type PdbAsset = NonNullable<RuntimeResources["pdb"]>[number];
export type SymbolsAsset = NonNullable<RuntimeResources["wasmSymbols"]>[number];

export const app = dotnet as unknown as ModuleAPI;
