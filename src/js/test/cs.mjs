import emb, { Test as EmbTest } from "./cs/Test/bin/embedded/bootsharp.mjs";
import sid, { Test as SidTest } from "./cs/Test/bin/sideload/bootsharp.mjs";
import assert from "node:assert";
import { resolve, parse, basename } from "node:path";
import { readdirSync, readFileSync, existsSync } from "node:fs";
import { pathToFileURL } from "node:url";

export const embedded = emb;
export const sideload = sid;
export const EmbeddedTest = EmbTest;
export const SideloadTest = SidTest;
export const root = pathToFileURL("./test/cs/Test/bin/sideload/bin").toString();

export * from "./cs/Test/bin/sideload/bootsharp.mjs";

assertPathExists("test/cs/Test/bin/embedded/bootsharp.mjs");
assertPathExists("test/cs/Test/bin/sideload/bootsharp.mjs");

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

function loadWasmBinary() {
    const file = resolve("test/cs/Test/bin/sideload/bin/dotnet.native.wasm");
    assertPathExists(file);
    return readFileSync(file);
}

function loadAssemblies() {
    let assemblies = [];
    for (const assemblyPath of findAssemblies())
        assemblies.push(loadAssembly(assemblyPath));
    return assemblies;
}

function findAssemblies() {
    let assemblyPaths = [];
    const dirPath = resolve("test/cs/Test/bin/sideload/bin");
    assertPathExists(dirPath);
    for (const fileName of readdirSync(dirPath))
        if (!fileName.endsWith("dotnet.native.wasm") && fileName.endsWith(".wasm"))
            assemblyPaths.push(`${dirPath}/${fileName}`);
    return assemblyPaths;
}

function loadAssembly(assemblyPath) {
    return {
        name: parse(assemblyPath).base,
        content: readFileSync(assemblyPath)
    };
}

function assertPathExists(pathToCheck) {
    const name = basename(pathToCheck);
    assert(existsSync(pathToCheck), `Missing test project artifact: '${name}'. Run 'scripts/compile-test.sh'.`);
}
