// @ts-nocheck (resolved when building C# solution)
import * as _runtime from "./dotnet.runtime.js";
import * as _native from "./dotnet.native.js";
import { dotnet, exit as _exit } from "./dotnet.js";
import type { DotnetHostBuilder, exit as t_exit, MonoConfig, AssetEntry } from "./dotnet-types";

export type RuntimeConfig = MonoConfig & { assets: AssetEntry[] };
export const builder: DotnetHostBuilder = dotnet;
export const exit: typeof t_exit = _exit;
export const runtime = _runtime;
export const native = _native;
