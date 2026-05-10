import assert from "node:assert";
import { resolve } from "node:path";
import { readFileSync, existsSync } from "node:fs";
import type { BootResources } from "./cs/Test";
import bootsharp, { Test } from "./cs/Test";

export { bootsharp, Test };
export * from "./cs/Test";

export const resources: BootResources = loadResources();

export async function bootRuntime() {
    Test.Program.onMainInvoked = () => {};
    await bootsharp.boot({ resources });
}

export function getDeclarations() {
    const file = resolvePath("test/cs/Test/bin/bootsharp/types/generated/bindings.g.d.mts");
    return readFileSync(file).toString();
}

function loadResources(): BootResources {
    return {
        wasm: load(bootsharp.resources.wasm.name),
        assemblies: bootsharp.resources.assemblies.map(r => load(r.name)),
        icu: bootsharp.resources.icu.map(r => load(r.name)),
        symbols: bootsharp.resources.symbols.map(r => load(r.name)),
        pdb: bootsharp.resources.pdb.map(r => load(r.name)),
        entryAssemblyName: bootsharp.resources.entryAssemblyName
    };
}

function load(name: string) {
    const path = resolvePath(`test/cs/Test/bin/bootsharp/bin/${name}`);
    const bytes = readFileSync(path);
    const content = bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength) as ArrayBuffer;
    return { name, content };
}

function resolvePath(path: string) {
    const resolved = resolve(path);
    assert(existsSync(resolved), `Missing test project artifact: '${path}'. Run 'scripts/compile-test.sh'.`);
    return resolved;
}
