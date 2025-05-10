1. Install Zig https://ziglang.org/download/
2. Install Binaryen https://github.com/WebAssembly/binaryen
3. Run `zig build -Doptimize=ReleaseFast`
4. Run `wasm-opt zig-out/bin/zig.wasm -O3 -o zig-out/bin/zig.wasm --all-features --strip-dwarf --strip-debug --vacuum`
