cd ..
printf "Removing previous installation...\n"
rm -rf native/runtime
mkdir -p native/runtime
printf "Downloading the runtime sources...\n"
# The fork modifies emscripten compilation flags to support UMD.
curl -L https://github.com/Elringus/DotNetUMD/archive/release/6.0.tar.gz | tar xz -C native/runtime --strip-components=1
printf "Compiling native modules. Initial run will take a while...\n"
source native/emsdk/emsdk_env.sh
native/runtime/build.sh mono+libs -os Browser -c Release
cp -f native/runtime/artifacts/bin/native/net6.0-Browser-Release-wasm/dotnet.js native/dotnet.js
cp -f native/runtime/artifacts/bin/native/net6.0-Browser-Release-wasm/dotnet.wasm native/dotnet.wasm
# Apply a patch to make dotnet.js work in strict mode
patch -u native/dotnet.js -i scripts/strict.patch
# Strip environment-specific statements from the autogenerated js wrapper.
sed -i "s/require([^)]*./{}/g" native/dotnet.js
sed -i "s/performance.now()/0/g" native/dotnet.js
sed -i "s/console.debug(\"mono_wasm_runtime_ready.\+)/{}/g" native/dotnet.js
read -r -p "Press Enter key to exit..."
