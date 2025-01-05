import emb, { Test as EmbTest } from "./cs/Test/bin/embedded";
import sid, { Test as SidTest } from "./cs/Test/bin/sideload";
import assert from "node:assert";
import { resolve, parse, basename } from "node:path";
import { readdirSync, readFileSync, existsSync } from "node:fs";

export const embedded = emb;
export const sideload = sid;
export const EmbeddedTest = EmbTest;
export const SideloadTest = SidTest;
export const root = "./test/cs/Test/bin/sideload/bin";

export * from "./cs/Test/bin/sideload";

assertPathExists("test/cs/Test/bin/embedded/index.mjs");
assertPathExists("test/cs/Test/bin/sideload/index.mjs");

export const bins = {
    wasm: loadWasmBinary(),
    assemblies: loadAssemblies(),
    entryAssemblyName: "Test.dll"
};

export async function bootEmbedded() {
    EmbeddedTest.Program.onMainInvoked = () => {};
    await embedded.boot({});
}

export async function bootSideload() {
    SideloadTest.Program.onMainInvoked = () => {};
    await sideload.boot({ root });
}

export function getDeclarations() {
    const file = resolve("test/cs/Test/bin/embedded/types/bindings.g.d.ts");
    assertPathExists(file);
    return readFileSync(file).toString();
}

// Just casting to <any> triggers codefactor.
export function any(obj: unknown): Record<string, unknown> {
    return <Record<string, unknown>><unknown>obj;
}

export function to<T>(obj: unknown): Record<string, T> {
    return <Record<string, T>><unknown>obj;
}

function loadWasmBinary() {
    const file = resolve("test/cs/Test/bin/sideload/bin/dotnet.native.wasm");
    assertPathExists(file);
    return readFileSync(file);
}

function loadAssemblies() {
    const assemblies = [];
    for (const assemblyPath of findAssemblies())
        assemblies.push(loadAssembly(assemblyPath));
    return assemblies;
}

function findAssemblies() {
    const assemblyPaths = [];
    const dirPath = resolve("test/cs/Test/bin/sideload/bin");
    assertPathExists(dirPath);
    for (const fileName of readdirSync(dirPath))
        if (!fileName.endsWith("dotnet.native.wasm") && fileName.endsWith(".wasm"))
            assemblyPaths.push(`${dirPath}/${fileName}`);
    return assemblyPaths;
}

function loadAssembly(assemblyPath: string) {
    return {
        name: parse(assemblyPath).base,
        content: readFileSync(assemblyPath)
    };
}

function assertPathExists(pathToCheck: string) {
    const name = basename(pathToCheck);
    assert(existsSync(pathToCheck), `Missing test project artifact: '${name}'. Run 'scripts/compile-test.sh'.`);
}
