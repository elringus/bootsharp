import bootsharp, { Program } from "./cs/bin/bootsharp/index.mjs";
import { pathToFileURL } from "node:url";
import fs from "node:fs/promises";
import zlib from "node:zlib";
import util from "node:util";
import path from "node:path";

console.log(`Binary size: ${await measure("./cs/bin/bootsharp/bin")}KB`);
console.log(`Brotli size: ${await measure("./cs/bin/bootsharp/bro")}KB`);

const resources = { ...bootsharp.resources };
await Promise.all([
    fetchBro(resources.wasm),
    ...resources.assemblies.map(fetchBro)
]);

Program.log = console.log;
const root = pathToFileURL(path.resolve("./cs/bin/bootsharp/bin"));
await bootsharp.boot({ root, resources });

async function measure(dir) {
    let size = 0;
    for await (const entry of await fs.opendir(dir))
        size += (await fs.stat(`${entry.path}/${entry.name}`)).size;
    return Math.ceil(size / 1024);
}

async function fetchBro(resource) {
    const bro = await fs.readFile(`./cs/bin/bootsharp/bro/${resource.name}.br`);
    resource.content = await util.promisify(zlib.brotliDecompress)(bro);
}
