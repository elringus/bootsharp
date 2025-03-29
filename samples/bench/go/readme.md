1. Install Go https://go.dev/dl
2. Copy `{GO_INSTALL_DIR}/lib/wasm/wasm_exec.js` to this folder
3. Run `& { $env:GOOS="js"; $env:GOARCH="wasm"; go build -o main.wasm main.go }`
