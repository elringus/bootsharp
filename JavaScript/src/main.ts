import { boot, exit } from "./boot";
import { resources } from "./resources";
import { builder, runtime, native } from "./dotnet-api";

export default { boot, exit, resources, dotnet: { builder, runtime, native } };
// @ts-ignore (resolved when building C# solution)
export * from "./bootsharp-bindings.js";
