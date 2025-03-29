## Setup

1. Build each sub-dir (readme inside)
2. Run `npm bench.mjs` to bench all
3. Or `npm bench.mjs rust|net|boot|go`

## Benches

- `Echo Number` — interop with raw numbers
- `Echo Struct` — interop with JSON-serialized structs
- `Fibonacci` — compute performance

All results are in milliseconds; lower is better.

## 2024 (.NET 9)

|             | Rust    | .NET    | Bootsharp | Go      |
|-------------|---------|---------|-----------|---------|
| Echo Number | `00.02` | `00.42` | `00.47`   | `15.38` |
| Echo Struct | `02.45` | `10.77` | `11.43`   | `52.84` |
| Fibonacci   | `15.11` | `24.57` | `24.45`   | `66.38` |
