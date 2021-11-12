import { boot, getBootStatus, terminate, BootStatus, BootData } from "./boot";
import { invoke, invokeAsync } from "./interop";
import { Assembly } from "./mono";

export const dotnet = {
    BootStatus: BootStatus,
    getBootStatus: getBootStatus,
    boot: boot,
    terminate: terminate,
    invoke: invoke,
    invokeAsync: invokeAsync
};

export { BootStatus, BootData, Assembly };
export { boot, getBootStatus, terminate, invoke, invokeAsync };
