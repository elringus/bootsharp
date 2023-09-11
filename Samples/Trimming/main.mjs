import bootsharp, { Backend, Frontend } from "./cs/bin/bootsharp/bootsharp.js";
import zlib from "node:zlib";
import util from "node:util";
import fs from "node:fs/promises";

console.log(`Binary size: ${await measure("./cs/bin/bootsharp/bin")}KB`);
console.log(`Brotli size: ${await measure("./cs/bin/bootsharp/bro")}KB`);

await Promise.all([
    fetchBro(bootsharp.resources.wasm),
    ...bootsharp.resources.assemblies.map(fetchBro)
]);

Frontend.log = console.log;
Frontend.getInfo = () => ({ environment: `Node ${process.version}` });
await bootsharp.boot();
console.log(`Backend: ${Backend.getInfo().environment}`);

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
