## Setup

1. Build each sub-dir (readme inside)
2. Run `node --expose-gc bench.mjs` to bench all
3. Add `rust|zig|llvm|net|boot|go` to bench specific

## Benches

- `Fibonacci` — compute with heavy recursion
- `Echo Number` — interop with raw numbers
- `Echo Struct` — interop with custom structs

All results are relative to the Rust baseline (lower is better).

## Bootsharp 0.3 (.NET 9, Node.js 22)

|             | Rust  | Zig   | .NET LLVM | Bootsharp | .NET AOT | Go      |
|-------------|-------|-------|-----------|-----------|----------|---------|
| Fibonacci   | `1.0` | `0.9` | `1.0`     | `1.0`     | `1.6`    | `3.7`   |
| Echo Number | `1.0` | `0.8` | `1.6`     | `14.0`    | `23.5`   | `718.7` |
| Echo Struct | `1.0` | `1.0` | `2.0`     | `2.5`     | `5.9`    | `15.2`  |

## Bootsharp 0.7 (.NET 10, Node.js 24)

|             | Rust  | Zig   | .NET LLVM | Bootsharp | .NET AOT | Go      |
|-------------|-------|-------|-----------|-----------|----------|---------|
| Fibonacci   | `1.0` | `2.0` | `1.2`     | `1.2`     | `2.1`    | `6.1`   |
| Echo Number | `1.0` | `0.9` | `1.6`     | `19.1`    | `28.6`   | `1057.0` |
| Echo Struct | `1.0` | `1.2` | `1.9`     | `2.5`     | `6.3`    | `18.8`  |

## Bootsharp 0.8 (.NET 10, Node.js 24)

|             | Rust  | Zig   | .NET LLVM | Bootsharp | .NET AOT | Go      |
|-------------|-------|-------|-----------|-----------|----------|---------|
| Fibonacci   | `1.0` | `2.0` | `1.2`     | `1.2`     | `2.1`    | `6.0`   |
| Echo Number | `1.0` | `1.0` | `1.6`     | `18.9`    | `28.2`   | `1068.1` |
| Echo Struct | `1.0` | `1.4` | `2.2`     | `1.1`     | `7.7`    | `21.2`  |
