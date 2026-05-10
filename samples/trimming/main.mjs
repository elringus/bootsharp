import bootsharp, { Program } from "./cs/bin/bootsharp/index.mjs";
import fs from "node:fs/promises";
import zlib from "node:zlib";
import util from "node:util";
import path from "node:path";

console.log(`Binary size: ${await measure("./cs/bin/bootsharp/bin")}KB`);
console.log(`Brotli size: ${await measure("./cs/bin/bootsharp/bro")}KB`);

Program.log = console.log;
await bootsharp.boot({ wasm: await loadBro(bootsharp.manifest.wasm) });

async function measure(dir) {
    let size = 0;
    for await (const entry of await fs.opendir(dir))
        size += (await fs.stat(path.join(dir, entry.name))).size;
    return Math.ceil(size / 1024);
}

async function loadBro(name) {
    const bro = await fs.readFile(`./cs/bin/bootsharp/bro/${name}.br`);
    const buf = await util.promisify(zlib.brotliDecompress)(bro);
    return buf.buffer.slice(buf.byteOffset, buf.byteOffset + buf.byteLength);
}
