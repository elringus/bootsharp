import { boot, getBootStatus, terminate, BootStatus, BootData } from "./boot";
import { invoke, invokeAsync } from "./interop";
import { Assembly } from "./mono";
export declare const dotnet: {
    BootStatus: typeof BootStatus;
    getBootStatus: typeof getBootStatus;
    boot: typeof boot;
    terminate: typeof terminate;
    invoke: <T>(assembly: string, method: string, ...args: any[]) => T;
    invokeAsync: <T_1>(assembly: string, method: string, ...args: any[]) => Promise<T_1>;
};
export { BootStatus, BootData, Assembly };
export { boot, getBootStatus, terminate, invoke, invokeAsync };
