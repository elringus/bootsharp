import assert from "node:assert";
import { resolve } from "node:path";
import { readFileSync, existsSync } from "node:fs";
import type { BootResources, BinaryResource } from "./cs/Test";
import bootsharp, { Test } from "./cs/Test";

export { bootsharp, Test };
export * from "./cs/Test";

export const resources = loadResources();
export const manifest = bootsharp.manifest;

export async function bootRuntime() {
    Test.Program.onMainInvoked = () => {};
    await bootsharp.boot(resources);
}

export function getDeclarations() {
    const file = resolvePath("test/cs/Test/bin/bootsharp/types/generated/bindings.g.d.mts");
    return readFileSync(file).toString();
}

function loadResources(): BootResources {
    return {
        wasm: load(bootsharp.manifest.wasm).content,
        assemblies: bootsharp.manifest.assemblies.map(load),
        icu: bootsharp.manifest.icu.map(load),
        symbols: bootsharp.manifest.symbols.map(load),
        pdb: bootsharp.manifest.pdb.map(load)
    };
}

function load(name: string): BinaryResource {
    const path = resolvePath(`test/cs/Test/bin/bootsharp/bin/${name}`);
    const bytes = readFileSync(path);
    const content = bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength) as ArrayBuffer;
    return { name, content };
}

function resolvePath(path: string): string {
    const resolved = resolve(path);
    assert(existsSync(resolved), `Missing test project artifact: '${path}'. Run 'scripts/compile-test.sh'.`);
    return resolved;
}
