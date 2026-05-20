import assert from "node:assert";
import { resolve } from "node:path";
import { readFileSync, readdirSync, statSync, existsSync } from "node:fs";
import bootsharp, { BootResources, BinaryResource } from "./cs/Test/bin/bootsharp/index.mjs";
import { Program } from "./cs/Test/bin/bootsharp/generated/modules/test.g.mjs";

export { bootsharp };
export * from "./cs/Test/bin/bootsharp/index.mjs";

export const resources = loadResources();
export const manifest = bootsharp.manifest;

export async function bootRuntime() {
    Program.onMainInvoked = () => {};
    await bootsharp.boot(resources);
}

export function getDeclarations() {
    const dir = resolvePath("test/cs/Test/bin/bootsharp/generated");
    return readAllDeclarations(dir);
}

function readAllDeclarations(dir: string): string {
    let out = "";
    for (const entry of readdirSync(dir)) {
        const full = `${dir}/${entry}`;
        if (statSync(full).isDirectory()) out += readAllDeclarations(full);
        else if (entry.endsWith(".g.d.mts")) out += readFileSync(full).toString();
    }
    return out;
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
