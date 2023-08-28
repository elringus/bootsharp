// @ts-ignore (resolved when building C# solution)
import * as runtimeModule from "./dotnet.runtime.js";
// @ts-ignore (resolved when building C# solution)
import * as nativeModule from "./dotnet.native.js";
// @ts-ignore (resolved when building C# solution)
import * as dotnetModule from "./dotnet.js";
import type { DotnetHostBuilder, MonoConfig, AssetEntry, ModuleAPI } from "./dotnet-types";

export type * from "./dotnet-types";
export type RuntimeConfig = MonoConfig & { assets: AssetEntry[] };
export const runtime = runtimeModule;
export const native = nativeModule;
export const dotnet: ModuleAPI = dotnetModule;
export const builder: DotnetHostBuilder = dotnet.dotnet;
