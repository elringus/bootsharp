import { boot, exit, getStatus, BootStatus } from "./boot";
import { getMain, getNative, getRuntime } from "./modules";
import { resources } from "./resources";
import { buildConfig } from "./config";

export default {
    boot,
    exit,
    getStatus,
    BootStatus,
    resources,
    /** .NET internal modules and associated utilities. */
    dotnet: { getMain, getNative, getRuntime, buildConfig }
};

export * from "./event";
export * from "./bindings.g";
export type { BootOptions } from "./boot";
export type { BootResources, BinaryResource } from "./resources";
