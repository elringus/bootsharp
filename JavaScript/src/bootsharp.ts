import { boot, exit, getStatus, BootStatus } from "./boot";
import { builder, module } from "./external";
import { resources } from "./resources";

export default { boot, exit, getStatus, BootStatus, resources, dotnet: { builder, module } };
export * from "./event";
export * from "./bindings.g";
export type { BootCustom } from "./boot";
export type { BootResources, BinaryResource } from "./resources";
