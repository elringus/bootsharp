// @ts-nocheck (resolved at C# solution build time)
import * as runtime from "./dotnet.runtime.js";
import * as native from "./dotnet.native.js";
import { dotnet as _dotnet, exit as _exit } from "./dotnet.js";
import type { DotnetHostBuilder, exit as t_exit, MonoConfig, AssetEntry } from "./dotnet-types";

export type RuntimeConfig = MonoConfig & AssetEntry[];
export const dotnet: DotnetHostBuilder = _dotnet;
export const exit: typeof t_exit = _exit;
