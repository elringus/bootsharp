import { boot, exit, getStatus, BootStatus } from "./boot";
import { resources } from "./resources";
import { builder, runtime, native, module } from "./external";

export default { boot, exit, getStatus, BootStatus, resources, dotnet: { builder, runtime, native, module } };
export * from "./event";
export * from "./bindings.g";
export type { BootCustom } from "./boot";
export type { BootResources, BinaryResource } from "./resources";
