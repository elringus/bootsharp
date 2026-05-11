import { boot, exit, getStatus, BootStatus } from "./boot.mjs";
import { manifest } from "./resources.mjs";
import { app } from "./dotnet/index.mjs";

export default {
    boot,
    exit,
    getStatus,
    BootStatus,
    manifest,
    dotnet: app.dotnet
};

export * from "./event.mjs";
export * from "./generated/index.g.mjs";
export type { BootOptions } from "./boot.mjs";
export type { BootManifest, BootResources, BinaryResource } from "./resources.mjs";
