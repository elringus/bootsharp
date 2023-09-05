import { boot, exit } from "./boot";
import { resources } from "./resources";
import { builder, runtime, native } from "./external";

export default { boot, exit, resources, dotnet: { builder, runtime, native } };
export * from "./bindings.g";
