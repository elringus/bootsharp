Example on producing minimal possible build size by disabling binaries embedding and utilizing aggressive trimming.

To test and measure build size:

- Run `dotnet publish cs`;
- Run `node main.mjs`.

### Measurements (KB)

| Bootsharp   | Raw   | Brotli |
|-------------|-------|--------|
| 0.1 .NET 8  | 2,298 | 739    |
| 0.7 .NET 9  | 1,737 | 518    |
| 0.8 .NET 10 | 1,610 | 482    |
