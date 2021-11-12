# https://github.com/dotnet/runtime/blob/release/6.0/src/mono/wasm/emscripten-version.txt
cd ..
rm -rf emsdk
mkdir emsdk
curl -L https://github.com/emscripten-core/emsdk/archive/2.0.23.tar.gz | tar xz -C "./emsdk" --strip-components=1
cd emsdk
./emsdk update
./emsdk install 2.0.23
./emsdk activate 2.0.23
read -p "Press Enter key to exit..."
