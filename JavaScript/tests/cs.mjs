import assert from "node:assert";
import path from "node:path";
import fs from "node:fs";
import bootsharp, { Test } from "./cs/Test/bin/bootsharp/bootsharp.js";

export default bootsharp;
export * from "./cs/Test/bin/bootsharp/bootsharp.js";

assertPathExists("tests/cs/Test/bin/bootsharp/bootsharp.js");

export const bins = {
    wasm: loadWasmBinary(),
    assemblies: loadAssemblies(),
    entryAssemblyName: "Test.dll"
};

export async function boot() {
    Test.onMainInvoked = () => {};
    bootsharp.resources.wasm.content = bins.wasm;
    for (const asm of bootsharp.resources.assemblies)
        asm.content = bins.assemblies.find(a => a.name === asm.name).content;
    await bootsharp.boot({});
}

export function exit() {
    setTimeout(bootsharp.exit, 0);
}

function getDeclarations() {
    const file = path.resolve("tests/cs/Test/bin/bootsharp/types/bindings.g.d.ts");
    assertPathExists(file);
    return fs.readFileSync(file).toString();
}

function loadWasmBinary() {
    const file = path.resolve("tests/cs/Test/bin/bootsharp/bin/dotnet.native.wasm");
    assertPathExists(file);
    return fs.readFileSync(file);
}

function loadAssemblies() {
    let assemblies = [];
    for (const assemblyPath of findAssemblies())
        assemblies.push(loadAssembly(assemblyPath));
    return assemblies;
}

function findAssemblies() {
    let assemblyPaths = [];
    const dirPath = path.resolve("tests/cs/Test/bin/bootsharp/bin");
    assertPathExists(dirPath);
    for (const fileName of fs.readdirSync(dirPath))
        if (!fileName.endsWith("dotnet.native.wasm") && fileName.endsWith(".wasm"))
            assemblyPaths.push(`${dirPath}/${fileName}`);
    return assemblyPaths;
}

function loadAssembly(assemblyPath) {
    return {
        name: path.parse(assemblyPath).base,
        content: fs.readFileSync(assemblyPath)
    };
}

function assertPathExists(pathToCheck) {
    const name = path.basename(pathToCheck);
    assert(fs.existsSync(pathToCheck), `Missing test project artifact: '${name}'. Run 'scripts/compile-test.sh'.`);
}
