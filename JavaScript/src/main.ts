import { boot, exit } from "./boot";
import { resources } from "./resources";
import { builder, runtime, native } from "./dotnet-api";

export { boot, exit, resources };
export const dotnet = { builder, runtime, native };
