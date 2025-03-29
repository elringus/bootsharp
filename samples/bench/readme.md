## Setup

1. Build each sub-dir (readme inside)
2. Run `npm bench.mjs` to bench all
3. Or `npm bench.mjs rust|llvm|net|boot|go`

## Benches

- `Echo Number` — interop with raw numbers
- `Echo Struct` — interop with JSON-serialized structs
- `Fibonacci` — compute performance

All results are relative to the Rust baseline (lower is better).

## 2024 (.NET 9)

|             | Rust  | .NET LLVM | .NET   | Bootsharp | Go      |
|-------------|-------|-----------|--------|-----------|---------|
| Echo Number | `1.0` | `14.4`    | `21.1` | `25.7`    | `718.7` |
| Echo Struct | `1.0` | `1.5`     | `4.3`  | `4.9`     | `20.8`  |
| Fibonacci   | `1.0` | `1.1`     | `1.5`  | `1.6`     | `3.8`   |
