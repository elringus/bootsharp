1. Install .NET https://dotnet.microsoft.com/en-us/download
2. Install Binaryen https://github.com/WebAssembly/binaryen
3. Run `dotnet publish`
4. Run `wasm-opt bin/Release/net9.0-browser/browser-wasm/publish/dotnet.native.wasm -O3 -o bin/Release/net9.0-browser/browser-wasm/publish/dotnet.native.wasm --all-features --strip-dwarf --strip-debug --vacuum`

https://github.com/dotnet/runtime/issues/113979#issuecomment-2759220563
