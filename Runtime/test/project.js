const dotnet = require("../dist/dotnet");
const path = require("path");
const fs = require("fs");
const assert = require("assert");

exports.bootTest = bootTest;
exports.getBootData = getBootData;

async function bootTest() {
    const bootData = getBootData();
    await dotnet.boot(bootData);
}

function getBootData() {
    return {
        wasm: loadWasmBinary(),
        assemblies: loadAssemblies(),
        entryAssemblyName: "Test.dll"
    };
}

function loadWasmBinary() {
    const file = path.resolve("test/project/bin/Release/net6.0/publish/wwwroot/_framework/dotnet.wasm");
    assert(fs.existsSync(file), "Missing WASM binary. Run 'scripts/compile-runtime.sh'.");
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
    assert(fs.existsSync(dirPath), "Missing test assemblies. Run 'scripts/compile-test.sh'.");
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
