import { boot, exit, getStatus, BootStatus } from "./boot.mjs";
import { resources } from "./resources.mjs";
import { app } from "./dotnet/index.mjs";

export default {
    boot,
    exit,
    getStatus,
    BootStatus,
    resources,
    dotnet: app.dotnet
};

export * from "./event.mjs";
export * from "./generated/bindings.g.mjs";
export type { BootOptions } from "./boot.mjs";
export type { BootResources, BinaryResource } from "./resources.mjs";
