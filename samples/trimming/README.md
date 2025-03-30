Example on producing minimal possible build size by disabling binaries embedding and utilizing aggressive trimming.

To test and measure build size:
- Run `dotnet publish cs`;
- Run `node main.mjs`.

### Measurements (KB)

|             | Raw   | Brotli |
|-------------|-------|--------|
| .NET 8      | 2,298 | 739    |
| .NET 9 LLVM | 1,749 | 520    |
