const path = require("path");
const fs = require("fs");
const assert = require("assert");
const dotnet = require("../dist/dotnet");

assertPathExists("test/project/bin/dotnet.js");
exports.packed = require("./project/bin/dotnet");
exports.bootTest = bootTest;
exports.getBootData = getBootData;
exports.getGeneratedTypes = getGeneratedTypes;
exports.getGeneratedMap = getGeneratedMap;

async function bootTest() {
    const bootData = getBootData();
    await dotnet.boot(bootData);
}

function getBootData() {
    return {
        wasm: loadWasmBinary(),
        assemblies: loadAssemblies(),
        entryAssemblyName: "Test.Project.dll"
    };
}

function getGeneratedTypes() {
    const file = path.resolve("test/project/bin/dotnet.d.ts");
    assertPathExists(file);
    return fs.readFileSync(file).toString();
}

function getGeneratedMap() {
    const file = path.resolve("test/project/bin/dotnet.js.map");
    assertPathExists(file);
    return fs.readFileSync(file).toString();
}

function loadWasmBinary() {
    const file = path.resolve("native/dotnet.wasm");
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
    const dirPath = path.resolve("test/project/bin/Release/net6.0/publish/wwwroot/_framework");
    assertPathExists(dirPath);
    for (const fileName of fs.readdirSync(dirPath))
        if (fileName.endsWith(".dll"))
            assemblyPaths.push(`${dirPath}/${fileName}`);
    return assemblyPaths;
}

function loadAssembly(assemblyPath) {
    return {
        name: path.parse(assemblyPath).base,
        data: fs.readFileSync(assemblyPath)
    };
}

function assertPathExists(pathToCheck) {
    const name = path.basename(pathToCheck);
    assert(fs.existsSync(pathToCheck), `Missing test project artifact: '${name}'. Run 'scripts/compile-test.sh'.`);
}
