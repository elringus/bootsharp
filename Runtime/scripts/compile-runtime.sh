cd ..
printf "Removing previous installation...\n"
rm -rf runtime
mkdir runtime
printf "Downloading the runtime sources...\n"
curl -L https://github.com/Elringus/DotNetUMD/archive/release/6.0.tar.gz | tar xz -C "./runtime" --strip-components=1
printf "Compiling native modules. Initial run will take a while...\n"
source ./emsdk/emsdk_env.sh
./runtime/build.sh mono+libs -os Browser -c Release
sed -i "s/require([^)]*./{}/g" "./runtime/artifacts/bin/native/net6.0-Browser-Release-wasm/dotnet.js"
read -p "Press Enter key to exit..."
