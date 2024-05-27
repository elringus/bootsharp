To build and bench:

- Run `dotnet publish cs`;
- Run `hyperfine 'node xxx.mjs'`.

### Bootsharp v0.3.1 (.NET 8.0 )

```
Benchmark 1: node marshaling.mjs
  Time (mean ± σ):     10.229 s ±  0.004 s    [User: 3.003 s, System: 0.193 s]
  Range (min … max):   10.223 s … 10.236 s    10 runs
```

### Bootsharp v0.4.0 (.NET 8.0 )

```
Benchmark 1: node marshaling.mjs
  Time (mean ± σ):     10.217 s ±  0.006 s    [User: 1.458 s, System: 0.176 s]
  Range (min … max):   10.212 s … 10.233 s    10 runs
```
