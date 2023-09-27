import { boot, exit } from "./boot";
import { resources } from "./resources";
import { builder, runtime, native, module } from "./external";

export default { boot, exit, resources, dotnet: { builder, runtime, native, module } };
export * from "./event";
export * from "./bindings.g";
export type { BootCustom } from "./boot";
export type { BootResources, BinaryResource } from "./resources";
