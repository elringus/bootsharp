import assert from "node:assert";
import path from "node:path";
import fs from "node:fs";
import bootsharp from "./cs/Test/bin/bootsharp/bootsharp.js";

export default bootsharp;
export * from "./cs/Test/bin/bootsharp/bootsharp.js";

assertPathExists("tests/cs/Test/bin/bootsharp/bootsharp.js");

export function getBinaries() {
    return {
        wasm: loadWasmBinary(),
        assemblies: loadAssemblies(),
        entryAssemblyName: "Test.dll"
    };
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
