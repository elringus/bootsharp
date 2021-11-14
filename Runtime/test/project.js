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
        wasmBinary: loadWasmBinary(),
        assemblies: loadAssemblies(),
        entryAssemblyName: "Test.dll"
    };
}

function loadWasmBinary() {
    const dirPath = resolvePublishDirectory();
    return fs.readFileSync(`${dirPath}/dotnet.wasm`);
}

function loadAssemblies() {
    let assemblies = [];
    for (const assemblyPath of findAssemblies())
        assemblies.push(loadAssembly(assemblyPath));
    return assemblies;
}

function findAssemblies() {
    let assemblyPaths = [];
    const dirPath = resolvePublishDirectory();
    for (const fileName of fs.readdirSync(dirPath))
        if (fileName.endsWith(".dll"))
            assemblyPaths.push(`${dirPath}/${fileName}`);
    return assemblyPaths;
}

function resolvePublishDirectory() {
    const dirPath = path.resolve("test/project/bin/Release/net6.0/publish/js");
    assert(fs.existsSync(dirPath), "Missing test assemblies. Run 'scripts/compile-test.sh'.");
    return dirPath;
}

function loadAssembly(assemblyPath) {
    return {
        name: path.parse(assemblyPath).base,
        data: fs.readFileSync(assemblyPath)
    };
}
