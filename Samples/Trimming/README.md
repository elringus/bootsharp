Example on producing minimal possible build size via disabling binaries embedding, aggressive trimming, stripping non-essential .NET features and using source-generated JSON serialization instead of reflection-based.

To test and measure final build size:
- Run `dotnet publish cs/Trimming.csproj`;
- Run `node node.mjs`.
