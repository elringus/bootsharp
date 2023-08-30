// @ts-ignore (resolved when building C# solution)
import * as runtimeModule from "./dotnet.runtime";
// @ts-ignore (resolved when building C# solution)
import * as nativeModule from "./dotnet.native";
// @ts-ignore (resolved when building C# solution)
import * as dotnetModule from "./dotnet";
import type { DotnetHostBuilder, MonoConfig, AssetEntry, ModuleAPI } from "./dotnet.d.ts";

export type * from "./dotnet.d.ts";
export type RuntimeConfig = MonoConfig & { assets: AssetEntry[] };
export const runtime = runtimeModule;
export const native = nativeModule;
export const dotnet: ModuleAPI = dotnetModule;
export const builder: DotnetHostBuilder = dotnet.dotnet;
