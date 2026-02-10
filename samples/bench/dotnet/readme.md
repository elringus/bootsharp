1. Install .NET https://dotnet.microsoft.com/en-us/download
2. Install Binaryen https://github.com/WebAssembly/binaryen
3. Run `dotnet publish`
4. Run `wasm-opt bin/Release/net10.0/browser-wasm/AppBundle/_framework/dotnet.native.wasm -O3 -o bin/Release/net10.0/browser-wasm/AppBundle/_framework/dotnet.native.wasm --all-features --strip-dwarf --strip-debug --vacuum`
