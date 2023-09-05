import * as runtimeModule from "./dotnet.runtime";
import * as nativeModule from "./dotnet.native";
import * as dotnetModule from "./dotnet";
import type { DotnetHostBuilder, MonoConfig, AssetEntry, ModuleAPI } from "./dotnet.d.ts";

export type * from "./dotnet.d.ts";
export type RuntimeConfig = MonoConfig & { assets: AssetEntry[] };
export const runtime = runtimeModule;
export const native = nativeModule;
export const dotnet: ModuleAPI = dotnetModule;
export const builder: DotnetHostBuilder = dotnet.dotnet;
