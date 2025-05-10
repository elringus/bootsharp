## Setup

1. Build each sub-dir (readme inside)
2. Run `node --expose-gc bench.mjs` to bench all
3. Add `rust|zig|llvm|net|boot|go` to bench specific

## Benches

- `Fibonacci` — compute with heavy recursion
- `Echo Number` — interop with raw numbers
- `Echo Struct` — interop with JSON-serialized structs

All results are relative to the Rust baseline (lower is better).

## 2024 (.NET 9)

|             | Rust  | Zig   | .NET LLVM | Bootsharp | .NET AOT | Go      |
|-------------|-------|-------|-----------|-----------|----------|---------|
| Fibonacci   | `1.0` | `0.9` | `1.0`     | `1.0`     | `1.6`    | `3.7`   |
| Echo Number | `1.0` | `0.8` | `1.6`     | `14.0`    | `23.5`   | `718.7` |
| Echo Struct | `1.0` | `1.0` | `2.0`     | `2.5`     | `5.9`    | `15.2`  |
