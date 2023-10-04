import * as runtimeModule from "./dotnet.runtime.g";
import * as nativeModule from "./dotnet.native.g";
import * as dotnetModule from "./dotnet";
import type { DotnetHostBuilder, MonoConfig, AssetEntry, ModuleAPI } from "./dotnet.d.ts";

export type * from "./dotnet.d.ts";
export type RuntimeConfig = MonoConfig & { assets?: AssetEntry[] };
export const runtime = runtimeModule;
export const native = nativeModule;
export const module: ModuleAPI = dotnetModule;
export const builder: DotnetHostBuilder = module.dotnet;
