cd ..
rm -rf native/emsdk
mkdir -p native/emsdk
# Version: https://github.com/dotnet/runtime/blob/release/6.0/src/mono/wasm/emscripten-version.txt.
curl -L https://github.com/emscripten-core/emsdk/archive/2.0.23.tar.gz | tar xz -C native/emsdk --strip-components=1
native/emsdk/emsdk update
native/emsdk/emsdk install 2.0.23
native/emsdk/emsdk activate 2.0.23
read -r -p "Press Enter key to exit..."
