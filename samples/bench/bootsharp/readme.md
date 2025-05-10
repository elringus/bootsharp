1. Install .NET https://dotnet.microsoft.com/en-us/download
2. Install Binaryen https://github.com/WebAssembly/binaryen
3. Run `dotnet publish`
4. Run `wasm-opt bin/bootsharp/bin/dotnet.native.wasm -O3 -o bin/bootsharp/bin/dotnet.native.wasm --all-features --strip-dwarf --strip-debug --vacuum`
