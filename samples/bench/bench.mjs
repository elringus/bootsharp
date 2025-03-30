import { init as initBootsharp } from "./bootsharp/init.mjs";
import { init as initDotNet } from "./dotnet/init.mjs";
import { init as initDotNetLLVM } from "./dotnet-llvm/init.mjs";
import { init as initGo } from "./go/init.mjs";
import { init as initRust } from "./rust/init.mjs";

/**
 * @typedef {Object} Exports
 * @property {() => number} echoNumber
 * @property {() => Data} echoStruct
 * @property {(n: number) => number} fi
 */

const lang = process.argv[2];
const baseline = [];

if (!lang || lang.toLowerCase() === "rust")
    await run("Rust", await initRust());
if (!lang || lang.toLowerCase() === "llvm")
    await run(".NET LLVM", await initDotNetLLVM());
if (!lang || lang.toLowerCase() === "net")
    await run(".NET AOT", await initDotNet());
if (!lang || lang.toLowerCase() === "boot")
    await run("Bootsharp", await initBootsharp());
if (!lang || lang.toLowerCase() === "go")
    await run("Go", await initGo());

/** @param {string} lang
 *  @param {Exports} exports */
async function run(lang, exports) {
    console.log(`\n\nBenching ${lang}...\n`);
    console.log(`Echo number: ${iterate(0, exports.echoNumber, 100, 3, 1000)}`);
    console.log(`Echo struct: ${iterate(1, exports.echoStruct, 100, 3, 100)}`);
    console.log(`Fibonacci: ${iterate(2, () => exports.fi(33), 100, 3, 1)}`);
}

function iterate(idx, fn, iterations, warms, loops) {
    const results = [];
    warms *= -1;
    for (let i = warms; i < iterations; i++) {
        const start = performance.now();
        for (let l = 0; l < loops; l++) fn();
        if (i >= 0) results.push(performance.now() - start);
    }
    let media = median(results);
    if (baseline[idx]) return `${(media / baseline[idx]).toFixed(1)}`;
    else baseline[idx] = media;
    return `${media.toFixed(3)} ms`;
}

function median(numbers) {
    const sorted = [...numbers].sort((a, b) => a - b);
    const middle = Math.floor(sorted.length / 2);
    if (sorted.length % 2 === 1) return sorted[middle];
    return (sorted[middle - 1] + sorted[middle]) / 2;
}
