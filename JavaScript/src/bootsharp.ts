import { boot, exit, getStatus, BootStatus } from "./boot";
import { getMain, getNative, getRuntime } from "./modules";
import { resources } from "./resources";

export default {
    boot,
    exit,
    getStatus,
    BootStatus,
    resources,
    dotnet: { getMain, getNative, getRuntime }
};

export * from "./event";
export * from "./bindings.g";
export type { BootOptions } from "./boot";
export type { BootResources, BinaryResource } from "./resources";
