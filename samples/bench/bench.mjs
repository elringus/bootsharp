import { init as initBootsharp } from "./bootsharp/init.mjs";
import { init as initDotNet } from "./dotnet/init.mjs";
import { init as initDotNetLLVM } from "./dotnet-llvm/init.mjs";
import { init as initGo } from "./go/init.mjs";
import { init as initRust } from "./rust/init.mjs";
import { init as initZig } from "./zig/init.mjs";
import * as fixtures from "./fixtures.mjs";

/**
 * @typedef {Object} Exports
 * @property {() => number} echoNumber
 * @property {() => Data} echoStruct
 * @property {(n: number) => number} fi
 */

const lang = process.argv[2];
const baseline = new Map;

if (!lang || lang.toLowerCase() === "rust")
    await run("Rust", await initRust());
if (!lang || lang.toLowerCase() === "zig")
    await run("Zig", await initZig());
if (!lang || lang.toLowerCase() === "llvm")
    await run(".NET LLVM", await initDotNetLLVM());
if (!lang || lang.toLowerCase() === "boot")
    await run("Bootsharp", await initBootsharp());
if (!lang || lang.toLowerCase() === "net")
    await run(".NET AOT", await initDotNet());
if (!lang || lang.toLowerCase() === "go")
    await run("Go", await initGo());

/** @param {string} lang
 *  @param {Exports} exports */
async function run(lang, exports) {
    console.log(`\n\nBenching ${lang}...\n`);

    global.gc();
    await new Promise(r => setTimeout(r, 100));

    bench("Fibonacci", () => exports.fi(33), 100, 3, 1);
    bench("Echo number", exports.echoNumber, 100, 3, 100000, fixtures.getNumber());
    bench("Echo struct", exports.echoStruct, 100, 3, 1000, fixtures.getStruct());
}

function bench(name, fn, iters, warms, loops, expected = undefined) {
    if (expected) {
        expected = JSON.stringify(expected);
        const actual = JSON.stringify(fn());
        if (actual !== expected) {
            console.error(`Wrong result of '${name}'. Expected: ${expected} Actual: ${actual}`);
            return;
        }
    }

    const results = [];
    warms *= -1;
    for (let i = warms; i < iters; i++) {
        const start = performance.now();
        for (let l = 0; l < loops; l++) fn();
        if (i >= 0) results.push(performance.now() - start);
    }
    const med = getMedian(results, 0.3);
    const dev = getDeviation(results);
    if (baseline.has(name)) {
        const flr = Math.floor((med / baseline.get(name)) * 10) / 10;
        console.log(`${name}: ${(flr).toFixed(1)} ${dev}`);
    } else {
        baseline.set(name, med);
        console.log(`${name}: ${med.toFixed(3)} ms ${dev}`);
    }
}

function getMedian(numbers, trim) {
    const sorted = [...numbers].sort((a, b) => a - b);
    const trimAmount = Math.floor(sorted.length * trim);
    const trimmed = sorted.slice(trimAmount, sorted.length - trimAmount);
    return trimmed.reduce((sum, val) => sum + val, 0) / trimmed.length;
}

function getDeviation(numbers) {
    const mean = numbers.reduce((sum, val) => sum + val, 0) / numbers.length;
    const sqr = numbers.map(value => Math.pow(value - mean, 2));
    const variance = sqr.reduce((sum, val) => sum + val, 0) / numbers.length;
    const dev = Math.sqrt(variance);
    return `±${((dev / mean) * 100).toFixed(0)}%`;
}
