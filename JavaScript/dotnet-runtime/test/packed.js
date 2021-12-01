// noinspection JSUnresolvedFunction,JSCheckFunctionSignatures,JSUnresolvedVariable

const assert = require("assert");
const dotnet = require("./project/bin/dotnet");
const { getGeneratedTypes } = require("./project");

describe("packed library", () => {
    after(dotnet.terminate);
    it("throws on boot when a C#-declared function is missing implementation", async () => {
        await assert.rejects(dotnet.boot, /Function 'dotnet.Test.Project.EchoFunction' is not implemented\./);
    });
    it("allows providing implementation for functions declared in C#", () => {
        dotnet.Test.Project.EchoFunction = value => value;
    });
    it("can boot without specifying boot data", async () => {
        await assert.doesNotReject(dotnet.boot);
        assert.deepStrictEqual(dotnet.getBootStatus(), dotnet.BootStatus.Booted);
    });
    it("re-exports dotnet members", async () => {
        assert(dotnet.BootStatus instanceof Object);
        assert(dotnet.getBootStatus instanceof Function);
        assert(dotnet.terminate instanceof Function);
        assert(dotnet.invoke instanceof Function);
        assert(dotnet.invokeAsync instanceof Function);
        assert(dotnet.createObjectReference instanceof Function);
        assert(dotnet.disposeObjectReference instanceof Function);
        assert(dotnet.createStreamReference instanceof Function);
    });
    it("provides exposed C# methods grouped under assembly object", async () => {
        assert.deepStrictEqual(dotnet.Test.Project.JoinStrings("a", "b"), "ab");
        assert.deepStrictEqual(await dotnet.Test.Project.JoinStringsAsync("c", "d"), "cd");
    });
    it("can interop via functions declared in C#", async () => {
        assert.deepStrictEqual(dotnet.Test.Project.TestEchoFunction("a"), "a");
    });
    it("still can interop via strings", async () => {
        assert.deepStrictEqual(dotnet.invoke("Test.Project", "JoinStrings", "a", "b"), "ab");
        assert.deepStrictEqual(await dotnet.invokeAsync("Test.Project", "JoinStringsAsync", "a", "b"), "ab");
    });
    it("generates valid type definitions", () => {
        assert.deepStrictEqual(getGeneratedTypes(), expectedTypes);
    });
});

const expectedTypes = `export interface Assembly {
    name: string;
    data: Uint8Array | string;
}
export interface BootData {
    wasm: Uint8Array | string;
    assemblies: Assembly[];
    entryAssemblyName: string;
}
export declare enum BootStatus {
    Standby = "Standby",
    Booting = "Booting",
    Terminating = "Terminating",
    Booted = "Booted"
}
export declare function getBootStatus(): BootStatus;
export declare function boot(): Promise<void>;
export declare function terminate(): Promise<void>;
export declare const invoke: <T>(assembly: string, method: string, ...args: any[]) => T;
export declare const invokeAsync: <T>(assembly: string, method: string, ...args: any[]) => Promise<T>;
export declare const createObjectReference: (object: any) => any;
export declare const disposeObjectReference: (objectReference: any) => void;
export declare const createStreamReference: (buffer: Uint8Array | any) => any;
export declare const Test: { Project: {
    TestEchoFunction: (value: string) => string,
    InvokeVoid: () => void,
    Echo: (message: string) => string,
    JoinStrings: (a: string, b: string) => string,
    SumDoubles: (a: number, b: number) => number,
    AddDays: (date: Date, days: number) => Date,
    InvokeJS: (funcName: string) => void,
    ForEachJS: (items: any, funcName: string) => any,
    JoinStringsAsync: (a: string, b: string) => Promise<string>,
    ReceiveBytes: (bytes: any) => string,
    SendBytes: () => string,
    CreateInstance: () => any,
    GetAndReturnJSObject: () => any,
    InvokeOnJSObjectAsync: (obj: any, fn: string, args: any) => Promise<void>,
    GetGuid: () => string,
    CatchException: () => string,
    Throw: (message: string) => string,
    ComputePrime: (n: number) => number,
    IsMainInvoked: () => boolean,
    StreamFromJSAsync: (streamRef: any) => Promise<void>,
    StreamFromDotNet: () => any,
    EchoFunction: (value: string) => string,
};};
`;
