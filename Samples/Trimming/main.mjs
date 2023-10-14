import bootsharp, { Global } from "./cs/bin/bootsharp/bootsharp.mjs";
import zlib from "node:zlib";
import util from "node:util";
import fs from "node:fs/promises";

console.log(`Binary size: ${await measure("./cs/bin/bootsharp/bin")}KB`);
console.log(`Brotli size: ${await measure("./cs/bin/bootsharp/bro")}KB`);

const resources = { ...bootsharp.resources };
await Promise.all([
    fetchBro(resources.wasm),
    ...resources.assemblies.map(fetchBro)
]);

Global.log = console.log;
await bootsharp.boot({ root: "./bin", resources });

async function measure(dir) {
    let size = 0;
    for await (const entry of await fs.opendir(dir))
        size += (await fs.stat(entry.path)).size;
    return Math.ceil(size / 1024);
}

async function fetchBro(resource) {
    const bro = await fs.readFile(`./cs/bin/bootsharp/bro/${resource.name}.br`);
    resource.content = await util.promisify(zlib.brotliDecompress)(bro);
}
