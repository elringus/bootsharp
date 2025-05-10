1. Install Go https://go.dev/dl
2. Install Binaryen https://github.com/WebAssembly/binaryen
3. Copy `{GO_INSTALL_DIR}/lib/wasm/wasm_exec.js` to this folder
4. Run `& { $env:GOOS="js"; $env:GOARCH="wasm"; go build -o main.wasm main.go }`
5. Run `wasm-opt main.wasm -O3 -o main.wasm --all-features --strip-dwarf --strip-debug --vacuum`
