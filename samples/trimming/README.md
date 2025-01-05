Example on producing minimal possible build size by disabling binaries embedding and utilizing aggressive trimming.

To test and measure build size:
- Run `dotnet publish cs`;
- Run `node main.mjs`.

### Measurements (KB)

| .NET  | Raw   | Brotli |
|-------|-------|--------|
| 8.0.1 | 2,298 | 739    |
| 9.0.1 | 2,369 | 761    |
